using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws
{
    public static class DynamoConfig
    {
    }

    public class DynamoConverters
    {
        public static Func<Type, string> FieldTypeFn { get; set; }
        public static Func<object, DynamoMetadataTable, Dictionary<string, AttributeValue>> ToAttributeValuesFn { get; set; }
        public static Func<Type, object, AttributeValue> ToAttributeValueFn { get; set; }
        public static Func<AttributeValue, Type, object> FromAttributeValueFn { get; set; }
        public static Func<object, Type, object> ConvertValueFn { get; set; }

        public Dictionary<Type, IAttributeValueConverter> ValueConverters = new Dictionary<Type, IAttributeValueConverter>
        {
            {typeof(DateTime), new DateTimeConverter() },
        };

        public virtual string GetFieldName(PropertyInfo pi)
        {
            var dynoAttr = pi.FirstAttribute<DynamoDBPropertyAttribute>();
            if (dynoAttr != null && dynoAttr.AttributeName != null)
                return dynoAttr.AttributeName;

            var alias = pi.FirstAttribute<AliasAttribute>();
            if (alias != null && alias.Name != null)
                return alias.Name;

            return pi.Name;
        }

        public virtual string GetFieldType(Type type)
        {
            string fieldType;

            if (FieldTypeFn != null)
            {
                fieldType = FieldTypeFn(type);
                if (fieldType != null)
                    return fieldType;
            }

            if (DynamoMetadata.FieldTypeMap.TryGetValue(type, out fieldType))
                return fieldType;

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null && DynamoMetadata.FieldTypeMap.TryGetValue(nullable, out fieldType))
                return fieldType;

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
                return DynamoType.List;

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDictionary<,>)))
                return DynamoType.Map;

            if (type.IsUserType())
                return DynamoType.Map;

            return DynamoType.String;
        }

        public virtual object ConvertValue(object value, Type type)
        {
            if (type.IsInstanceOfType(value))
                return value;

            if (ConvertValueFn != null)
            {
                var to = ConvertValueFn(value, type);
                if (to != null)
                    return to;
            }

            var mapValue = value as Dictionary<string, AttributeValue>;
            if (mapValue != null)
                return FromMapAttributeValue(mapValue, type);

            var listValue = value as List<AttributeValue>;
            if (listValue != null)
                return FromListAttributeValue(listValue, type);

            return value.ConvertTo(type);
        }

        public virtual void GetHashAndRangeKeyFields(Type type, PropertyInfo[] props, out PropertyInfo hash, out PropertyInfo range)
        {
            hash = null;
            range = null;

            var compositeAttrs = type.AllAttributes<CompositeIndexAttribute>();
            if (compositeAttrs.Length > 0)
            {
                var idAttr = compositeAttrs.FirstOrDefault(x => x.Name == DynamoKey.Hash);
                if (idAttr != null)
                {
                    hash = props.FirstOrDefault(x => x.Name == idAttr.FieldNames[0]);
                }

                var rangeAttr = compositeAttrs.FirstOrDefault(x => x.Name == DynamoKey.Range);
                if (rangeAttr != null)
                {
                    range = props.FirstOrDefault(x => x.Name == rangeAttr.FieldNames[0]);
                }

                if (hash == null && range == null)
                {
                    var attr = compositeAttrs[0];
                    if (attr.FieldNames.Count == 2)
                    {
                        hash = props.FirstOrDefault(x => x.Name == attr.FieldNames[0]);
                    }
                    else if (attr.FieldNames.Count == 2)
                    {
                        hash = props.FirstOrDefault(x => x.Name == attr.FieldNames[0]);
                        range = props.FirstOrDefault(x => x.Name == attr.FieldNames[1]);
                    }
                }
            }

            if (hash == null)
            {
                hash = props.FirstOrDefault(x => x.HasAttribute<DynamoDBHashKeyAttribute>())
                     ?? props.FirstOrDefault(x =>
                         x.HasAttribute<PrimaryKeyAttribute>() ||
                         x.HasAttribute<AutoIncrementAttribute>())
                     ?? props.FirstOrDefault(x => x.Name.EqualsIgnoreCase(IdUtils.IdField))
                     ?? props[0];
            }
            if (range == null)
            {
                range = props.FirstOrDefault(x => x.HasAttribute<DynamoDBRangeKeyAttribute>())
                     ?? props.FirstOrDefault(x => x.Name == "RangeKey");
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(DynamoMetadataField field, object hash)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { field.Name, ToAttributeValue(field.Type, field.DbType, hash) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(DynamoMetadataTable table, object hash, object range)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { table.HashKey.Name, ToAttributeValue(table.HashKey.Type, table.HashKey.DbType, hash) },
                    { table.RangeKey.Name, ToAttributeValue(table.RangeKey.Type, table.RangeKey.DbType, range) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeValues(object instance, DynamoMetadataTable table)
        {
            if (ToAttributeValuesFn != null)
            {
                var ret = ToAttributeValuesFn(instance, table);
                if (ret != null)
                    return ret;
            }

            using (AwsClientUtils.GetJsScope())
            {
                var to = new Dictionary<string, AttributeValue>();

                foreach (var field in table.Fields)
                {
                    var value = field.GetValue(instance);
                    to[field.Name] = ToAttributeValue(field.Type, field.DbType, value);
                }

                return to;
            }
        }

        public virtual AttributeValue ToAttributeValue(Type fieldType, string dbType, object value)
        {
            if (ToAttributeValueFn != null)
            {
                var attrVal = ToAttributeValueFn(fieldType, value);
                if (attrVal != null)
                    return attrVal;
            }

            if (value == null)
                return new AttributeValue { NULL = true };

            var valueConverter = GetValueConverter(fieldType);
            if (valueConverter != null)
                return valueConverter.ToAttributeValue(value);

            switch (dbType)
            {
                case DynamoType.Number:
                    return new AttributeValue { N = value.ToString() };
                case DynamoType.Bool:
                    return new AttributeValue { BOOL = (bool)value };
                case DynamoType.Binary:
                    return value is MemoryStream
                        ? new AttributeValue { B = (MemoryStream)value }
                        : value is Stream
                            ? new AttributeValue { B = new MemoryStream(((Stream)value).ReadFully()) }
                            : new AttributeValue { B = new MemoryStream((byte[])value) };
                case DynamoType.NumberSet:
                    return new AttributeValue { NS = value.ConvertTo<List<string>>() };
                case DynamoType.StringSet:
                    return new AttributeValue { NS = value.ConvertTo<List<string>>() };
                case DynamoType.List:
                    return ToListAttributeValue(value);
                case DynamoType.Map:
                    return ToMapAttributeValue(value);
                default:
                    return new AttributeValue { S = value.ToJsv() };
            }
        }

        public virtual AttributeValue ToMapAttributeValue(object value)
        {
            var map = value as IDictionary
                ?? value.ToObjectDictionary();

            var to = new Dictionary<string, AttributeValue>();
            foreach (var key in map.Keys)
            {
                var x = map[key];
                to[key.ToString()] = x != null
                    ? ToAttributeValue(x.GetType(), GetFieldType(x.GetType()), x)
                    : new AttributeValue { NULL = true };
            }
            return new AttributeValue { M = to };
        }

        public virtual object FromMapAttributeValue(Dictionary<string, AttributeValue> map, Type type)
        {
            var table = DynamoMetadata.GetTable(type);

            var from = new Dictionary<string, object>();
            foreach (var field in table.Fields)
            {
                AttributeValue attrValue;
                if (!map.TryGetValue(field.Name, out attrValue))
                    continue;

                from[field.Name] = FromAttributeValue(attrValue, field.Type);
            }

            var to = from.FromObjectDictionary(type);
            return to;
        }

        public virtual AttributeValue ToListAttributeValue(object oList)
        {
            var list = (IEnumerable)oList;

            var values = list.Map(x => ToAttributeValue(x.GetType(), GetFieldType(x.GetType()), x));
            return new AttributeValue { L = values };
        }

        public virtual object FromListAttributeValue(List<AttributeValue> attrs, Type toType)
        {
            var elType = toType.GetCollectionType();
            var from = attrs.Map(x => FromAttributeValue(x, elType));
            var to = TranslateListWithElements.TryTranslateCollections(
                from.GetType(), toType, from);
            return to;
        }

        public virtual T FromAttributeValues<T>(DynamoMetadataTable table, Dictionary<string, AttributeValue> attributeValues)
        {
            var to = typeof(T).CreateInstance<T>();
            return PopulateFromAttributeValues(to, table, attributeValues);
        }

        public virtual T PopulateFromAttributeValues<T>(T to, DynamoMetadataTable table, Dictionary<string, AttributeValue> attributeValues)
        {
            foreach (var entry in attributeValues)
            {
                var field = table.Fields.FirstOrDefault(x => x.Name == entry.Key);
                if (field == null || field.SetValueFn == null)
                    continue;

                var attrValue = entry.Value;
                var fieldType = field.Type;

                var value = FromAttributeValue(attrValue, fieldType);
                if (value == null)
                    continue;

                field.SetValueFn(to, value);
            }

            return to;
        }

        IAttributeValueConverter GetValueConverter(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            IAttributeValueConverter valueConverter;
            ValueConverters.TryGetValue(type, out valueConverter);
            return valueConverter;
        }

        private object FromAttributeValue(AttributeValue attrValue, Type fieldType)
        {
            var valueConverter = GetValueConverter(fieldType);
            if (valueConverter != null)
                return valueConverter.FromAttributeValue(attrValue);

            var value = FromAttributeValueFn != null
                ? FromAttributeValueFn(attrValue, fieldType) ?? GetAttributeValue(attrValue)
                : GetAttributeValue(attrValue);

            return value == null
                ? null
                : ConvertValue(value, fieldType);
        }

        public virtual object GetAttributeValue(AttributeValue attr)
        {
            if (attr == null || attr.NULL)
                return null;
            if (attr.S != null)
                return attr.S;
            if (attr.N != null)
                return attr.N;
            if (attr.B != null)
                return attr.B;
            if (attr.IsBOOLSet)
                return attr.BOOL;
            if (attr.IsLSet)
                return attr.L;
            if (attr.IsMSet)
                return attr.M;
            if (attr.SS != null)
                return attr.SS;
            if (attr.NS != null)
                return attr.NS;
            if (attr.BS != null)
                return attr.BS;

            return null;
        }

    }

}
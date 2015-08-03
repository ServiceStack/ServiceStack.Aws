using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DocumentModel;
using ServiceStack.Aws.Support;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDb
{
    public static class DynamoUtils
    {
        public static Dictionary<string, DynamoDBEntry> ToDynamoDbEntryMap(this Type type)
        {
            return new Dictionary<string, DynamoDBEntry>();
        }

        public static Dictionary<Type, string> Set<T>(this Dictionary<Type, string> map, string value)
        {
            map[typeof(T)] = value;
            return map;
        }
    }

    public class DynamoMetadata
    {
        public static DynamoConverters Converters = new DynamoConverters();

        public static readonly Dictionary<Type, string> FieldTypeMap = new Dictionary<Type, string>()
            .Set<string>(DynamoType.String)
            .Set<bool>(DynamoType.Bool)
            .Set<byte[]>(DynamoType.Binary)
            .Set<Stream>(DynamoType.Binary)
            .Set<MemoryStream>(DynamoType.Binary)
            .Set<byte>(DynamoType.Number)
            .Set<sbyte>(DynamoType.Number)
            .Set<short>(DynamoType.Number)
            .Set<ushort>(DynamoType.Number)
            .Set<int>(DynamoType.Number)
            .Set<uint>(DynamoType.Number)
            .Set<long>(DynamoType.Number)
            .Set<ulong>(DynamoType.Number)
            .Set<float>(DynamoType.Number)
            .Set<double>(DynamoType.Number)
            .Set<decimal>(DynamoType.Number)
            .Set<HashSet<string>>(DynamoType.StringSet)
            .Set<HashSet<int>>(DynamoType.NumberSet)
            .Set<HashSet<long>>(DynamoType.NumberSet)
            .Set<HashSet<double>>(DynamoType.NumberSet)
            .Set<HashSet<float>>(DynamoType.NumberSet)
            .Set<HashSet<decimal>>(DynamoType.NumberSet);

        public static List<DynamoMetadataTable> Tables { get; set; }

        public static DynamoMetadataTable GetTable<T>()
        {
            return GetTable(typeof(T));
        }

        public static DynamoMetadataTable GetTable(Type table)
        {
            var metadata = Tables.FirstOrDefault(x => x.Type == table);
            if (metadata == null)
                throw new ArgumentNullException("table", "Table does not exist: " + table.Name);

            return metadata;
        }

        public static List<DynamoMetadataTable> RegisterTables(IEnumerable<Type> tables)
        {
            foreach (var table in tables)
            {
                RegisterTable(table);
            }
            return Tables;
        }

        public static DynamoMetadataTable RegisterTable(Type type)
        {
            if (Tables == null)
                Tables = new List<DynamoMetadataTable>();

            var table = Tables.FirstOrDefault(x => x.Type == type);
            if (table != null)
                return table;

            var alias = type.FirstAttribute<AliasAttribute>();
            var props = type.GetSerializableProperties();
            PropertyInfo hash, range;
            Converters.GetHashAndRangeKeyFields(type, props, out hash, out range);

            table = new DynamoMetadataTable
            {
                Type = type,
                Name = alias != null ? alias.Name : type.Name,
            };
            table.Fields = props.Map(p =>
                new DynamoMetadataField
                {
                    Table = table,
                    Type = p.PropertyType,
                    Name = Converters.GetFieldName(p),
                    DbType = Converters.GetFieldType(p.PropertyType),
                    IsHashKey = p == hash,
                    IsRangeKey = p == range,
                    SetValueFn = p.GetPropertySetterFn(),
                    GetValueFn = p.GetPropertyGetterFn(),
                }).ToArray();
            table.HashKey = table.Fields.First(x => x.IsHashKey);
            table.RangeKey = table.Fields.FirstOrDefault(x => x.IsRangeKey);

            Tables.Add(table);

            LicenseUtils.AssertValidUsage(LicenseFeature.Aws, QuotaType.Tables, Tables.Count);

            return table;
        }
    }

    public class DynamoMetadataField
    {
        public DynamoMetadataTable Table { get; set; }

        public Type Type { get; set; }

        public string Name { get; set; }

        public string DbType { get; set; }

        public bool IsHashKey { get; set; }

        public bool IsRangeKey { get; set; }

        public PropertyGetterDelegate GetValueFn { get; set; }

        public PropertySetterDelegate SetValueFn { get; set; }

        public object GetValue(object onInstance)
        {
            return this.GetValueFn == null ? null : this.GetValueFn(onInstance);
        }
    }

    public class DynamoMetadataTable
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public DynamoMetadataField[] Fields { get; set; }

        public DynamoMetadataField HashKey { get; set; }

        public DynamoMetadataField RangeKey { get; set; }
    }

}
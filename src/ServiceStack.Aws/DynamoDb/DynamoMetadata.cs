using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2.DataModel;
using ServiceStack.Aws.Support;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDb
{
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

        public static HashSet<DynamoMetadataType> Types;

        public static void Reset()
        {
            Types = null;
        }

        public static DynamoMetadataType GetTable<T>()
        {
            return GetTable(typeof(T));
        }

        public static DynamoMetadataType GetTable(Type table)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == table);
            if (metadata == null || !metadata.IsTable)
                throw new ArgumentNullException("table", "Table has not been registered: " + table.Name);

            return metadata;
        }

        public static DynamoMetadataType TryGetTable(Type table)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == table);
            if (metadata == null || !metadata.IsTable)
                return null;
            return metadata;
        }

        public static List<DynamoMetadataType> GetTables()
        {
            return Types == null
                ? new List<DynamoMetadataType>()
                : Types.Where(x => x.IsTable).ToList();
        }

        public static DynamoMetadataType GetType<T>()
        {
            return GetType(typeof(T));
        }

        public static DynamoMetadataType GetType(Type type)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == type);
            if (metadata != null)
                return metadata;

            if (type.IsValueType)
                return null;

            RegisterTypes(type);

            var metaType = Types.FirstOrDefault(x => x.Type == type);
            return metaType;
        }

        public static void RegisterTables(IEnumerable<Type> tables)
        {
            foreach (var table in tables)
            {
                RegisterTable(table);
            }
        }

        // Should only be called at StartUp
        public static DynamoMetadataType RegisterTable(Type type)
        {
            if (Types == null)
                Types = new HashSet<DynamoMetadataType>();

            Types.RemoveWhere(x => x.Type == type);

            var table = ToMetadataTable(type);
            Types.Add(table);

            LicenseUtils.AssertValidUsage(LicenseFeature.Aws, QuotaType.Tables, Types.Count);

            RegisterTypes(type.GetReferencedTypes());

            return table;
        }

        public static void RegisterTypes(params Type[] refTypes)
        {
            var metadatas = refTypes.Where(x => !x.IsValueType && !x.IsSystemType()).Map(ToMetadataType);

            // Make thread-safe to allow usage at runtime
            HashSet<DynamoMetadataType> snapshot, newCache;
            do
            {
                snapshot = Types;
                newCache = new HashSet<DynamoMetadataType>(Types);
                foreach (var metadata in metadatas)
                {
                    newCache.Add(metadata);
                }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref Types, newCache, snapshot), snapshot));
        }

        private static PropertyInfo[] GetTableProperties(Type type)
        {
            var props = type.GetSerializableProperties().Where(x => x.CanRead && x.CanWrite).ToArray();
            return props;
        }

        private static DynamoMetadataType ToMetadataType(Type type)
        {
            var alias = type.FirstAttribute<AliasAttribute>();
            var props = GetTableProperties(type);

            var metadata = new DynamoMetadataType
            {
                Type = type,
                Name = alias != null ? alias.Name : type.Name,
            };
            metadata.Fields = props.Map(p =>
                new DynamoMetadataField
                {
                    Parent = metadata,
                    Type = p.PropertyType,
                    Name = Converters.GetFieldName(p),
                    DbType = Converters.GetFieldType(p.PropertyType),
                    IsAutoIncrement = p.HasAttribute<AutoIncrementAttribute>(),
                    SetValueFn = p.GetPropertySetterFn(),
                    GetValueFn = p.GetPropertyGetterFn(),
                }).ToArray();

            return metadata;
        }

        private static DynamoMetadataType ToMetadataTable(Type type)
        {
            var alias = type.FirstAttribute<AliasAttribute>();
            var props = GetTableProperties(type);
            PropertyInfo hash, range;
            Converters.GetHashAndRangeKeyFields(type, props, out hash, out range);

            var provision = type.FirstAttribute<ProvisionedThroughputAttribute>();

            var metadata = new DynamoMetadataType
            {
                Type = type,
                IsTable = true,
                Name = alias != null ? alias.Name : type.Name,
                ReadCapacityUnits = provision != null ? provision.ReadCapacityUnits : (int?)null,
                WriteCapacityUnits = provision != null ? provision.WriteCapacityUnits : (int?)null,
            };
            metadata.Fields = props.Map(p =>
                new DynamoMetadataField
                {
                    Parent = metadata,
                    Type = p.PropertyType,
                    Name = Converters.GetFieldName(p),
                    DbType = Converters.GetFieldType(p.PropertyType),
                    IsHashKey = p == hash,
                    IsRangeKey = p == range,
                    IsAutoIncrement = p.HasAttribute<AutoIncrementAttribute>(),
                    SetValueFn = p.GetPropertySetterFn(),
                    GetValueFn = p.GetPropertyGetterFn(),
                }).ToArray();

            metadata.HashKey = metadata.Fields.FirstOrDefault(x => x.IsHashKey);
            metadata.RangeKey = metadata.Fields.FirstOrDefault(x => x.IsRangeKey);

            if (metadata.HashKey == null)
                throw new ArgumentException("Could not infer Hash Key in Table '{0}'".Fmt(type.Name));

            var hashField = metadata.HashKey.Name;

            metadata.LocalIndexes = props.Where(x => x.HasAttribute<IndexAttribute>()).Map(x =>
                new DynamoLocalIndex
                {
                    Name = "{0}{1}Index".Fmt(metadata.Name, x.Name),
                    HashKey = metadata.HashKey,
                    RangeKey = metadata.GetField(x.Name),
                    ProjectionType = DynamoProjectionType.Include,
                    ProjectedFields = new[] { x.Name },
                });

            metadata.GlobalIndexes = new List<DynamoGlobalIndex>();

            var references = type.AllAttributes<ReferencesAttribute>();
            foreach (var attr in references)
            {
                var localIndex = attr.Type.GetTypeWithGenericInterfaceOf(typeof(ILocalIndex<>));
                if (localIndex != null)
                    metadata.LocalIndexes.Add(CreateLocalIndex(type, metadata, hashField, attr.Type));

                var globalIndex = attr.Type.GetTypeWithGenericInterfaceOf(typeof(IGlobalIndex<>));
                if (globalIndex != null)
                    metadata.GlobalIndexes.Add(CreateGlobalIndex(type, metadata, attr.Type));
            }

            return metadata;
        }

        private static DynamoLocalIndex CreateLocalIndex(Type type, DynamoMetadataType metadata, string hashField, Type indexType)
        {
            var indexProps = indexType.GetPublicProperties();
            var indexProp = indexProps.FirstOrDefault(x =>
                x.HasAttribute<IndexAttribute>() || x.HasAttribute<DynamoDBRangeKeyAttribute>());

            if (indexProp == null)
                throw new ArgumentException("Missing [Index]. Could not infer Range Key in index '{0}'.".Fmt(indexType));

            var indexAlias = indexType.FirstAttribute<AliasAttribute>();
            var rangeKey = metadata.GetField(indexProp.Name);
            if (rangeKey == null)
                throw new ArgumentException("Range Key '{0}' was not found on Table '{1}'".Fmt(indexProp.Name, type.Name));

            return new DynamoLocalIndex
            {
                IndexType = indexType,
                Name = indexAlias != null ? indexAlias.Name : indexType.Name,
                HashKey = metadata.HashKey,
                RangeKey = rangeKey,
                ProjectionType = DynamoProjectionType.Include,
                ProjectedFields = indexProps.Where(x => x.Name != hashField).Select(x => x.Name).ToArray(),
            };
        }

        private static DynamoGlobalIndex CreateGlobalIndex(Type type, DynamoMetadataType metadata, Type indexType)
        {
            var indexProps = indexType.GetPublicProperties();

            PropertyInfo indexHash, indexRange;
            Converters.GetHashAndRangeKeyFields(indexType, indexProps, out indexHash, out indexRange);

            var hashKey = metadata.GetField(indexHash.Name);
            if (hashKey == null)
                throw new ArgumentException("Hash Key '{0}' was not found on Table '{1}'".Fmt(indexHash.Name, type.Name));

            if (indexRange == null)
                indexRange = indexProps.FirstOrDefault(x => x.HasAttribute<IndexAttribute>());

            if (indexRange == null)
                throw new ArgumentException("Could not infer Range Key in index '{0}'.".Fmt(indexType));

            var rangeKey = metadata.GetField(indexRange.Name);
            if (rangeKey == null)
                throw new ArgumentException("Range Key '{0}' was not found on Table '{1}'".Fmt(indexRange.Name, type.Name));

            var indexAlias = indexType.FirstAttribute<AliasAttribute>();

            var indexProvision = indexType.FirstAttribute<ProvisionedThroughputAttribute>();

            return new DynamoGlobalIndex
            {
                IndexType = indexType,
                Name = indexAlias != null ? indexAlias.Name : indexType.Name,
                HashKey = hashKey,
                RangeKey = rangeKey,
                ProjectionType = DynamoProjectionType.Include,
                ProjectedFields = indexProps.Where(x => x.Name != indexHash.Name).Select(x => x.Name).ToArray(),
                ReadCapacityUnits = indexProvision != null ? indexProvision.ReadCapacityUnits : metadata.ReadCapacityUnits,
                WriteCapacityUnits = indexProvision != null ? indexProvision.WriteCapacityUnits : metadata.WriteCapacityUnits,
            };
        }
    }

    public class DynamoMetadataType
    {
        public string Name { get; set; }

        public bool IsTable { get; set; }

        public Type Type { get; set; }

        public DynamoMetadataField[] Fields { get; set; }

        public DynamoMetadataField HashKey { get; set; }

        public DynamoMetadataField RangeKey { get; set; }

        public List<DynamoLocalIndex> LocalIndexes { get; set; }

        public List<DynamoGlobalIndex> GlobalIndexes { get; set; }

        public int? ReadCapacityUnits { get; set; }

        public int? WriteCapacityUnits { get; set; }

        public DynamoMetadataField GetField(string fieldName)
        {
            return Fields.FirstOrDefault(x => x.Name == fieldName);
        }

        public bool HasField(string fieldName)
        {
            return GetField(fieldName) != null;
        }

        public DynamoMetadataField GetField(Type type)
        {
            return Fields.FirstOrDefault(x => x.Type == type);
        }

        public DynamoIndex GetIndex(Type indexType)
        {
            return (DynamoIndex)this.LocalIndexes.FirstOrDefault(x => x.IndexType == indexType)
                ?? this.GlobalIndexes.FirstOrDefault(x => x.IndexType == indexType);
        }

        public DynamoIndex GetIndexByField(string fieldName)
        {
            return (DynamoIndex)this.LocalIndexes.FirstOrDefault(x => x.RangeKey != null && x.RangeKey.Name == fieldName)
                ?? this.GlobalIndexes.FirstOrDefault(x => x.RangeKey != null && x.RangeKey.Name == fieldName);
        }
    }

    public class DynamoMetadataField
    {
        public DynamoMetadataType Parent { get; set; }

        public Type Type { get; set; }

        public string Name { get; set; }

        public string DbType { get; set; }

        public bool IsHashKey { get; set; }

        public bool IsRangeKey { get; set; }

        public bool IsAutoIncrement { get; set; }

        public PropertyGetterDelegate GetValueFn { get; set; }

        public PropertySetterDelegate SetValueFn { get; set; }

        public object GetValue(object onInstance)
        {
            return this.GetValueFn == null ? null : this.GetValueFn(onInstance);
        }

        public object SetValue(object instance, object value)
        {
            if (SetValueFn == null)
                return value;

            if (value != null && value.GetType() != Type)
                value = Convert.ChangeType(value, Type);

            SetValueFn(instance, value);

            return value;
        }
    }

    public static class DynamoProjectionType
    {
        public const string KeysOnly = "KEYS_ONLY";
        public const string Include = "INCLUDE";
        public const string All = "ALL";
    }

    public class DynamoIndex
    {
        public Type IndexType { get; set; }
        public string Name { get; set; }
        public DynamoMetadataField HashKey { get; set; }
        public DynamoMetadataField RangeKey { get; set; }
        public string ProjectionType { get; set; }
        public string[] ProjectedFields { get; set; }
    }

    public class DynamoLocalIndex : DynamoIndex
    {
    }

    public class DynamoGlobalIndex : DynamoIndex
    {
        public long? ReadCapacityUnits { get; set; }
        public long? WriteCapacityUnits { get; set; }
    }
}
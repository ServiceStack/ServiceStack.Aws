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
using ServiceStack.Text.Common;

namespace ServiceStack.Aws.DynamoDb
{
    public static class DynamoConfig
    {
        public static bool IsReservedWord(string word)
        {
            return ReservedWords.Contains(word.ToUpper());
        }

        public static HashSet<string> ReservedWords = new HashSet<string>
        {
            "ABORT",
            "ABSOLUTE",
            "ACTION",
            "ADD",
            "AFTER",
            "AGENT",
            "AGGREGATE",
            "ALL",
            "ALLOCATE",
            "ALTER",
            "ANALYZE",
            "AND",
            "ANY",
            "ARCHIVE",
            "ARE",
            "ARRAY",
            "AS",
            "ASC",
            "ASCII",
            "ASENSITIVE",
            "ASSERTION",
            "ASYMMETRIC",
            "AT",
            "ATOMIC",
            "ATTACH",
            "ATTRIBUTE",
            "AUTH",
            "AUTHORIZATION",
            "AUTHORIZE",
            "AUTO",
            "AVG",
            "BACK",
            "BACKUP",
            "BASE",
            "BATCH",
            "BEFORE",
            "BEGIN",
            "BETWEEN",
            "BIGINT",
            "BINARY",
            "BIT",
            "BLOB",
            "BLOCK",
            "BOOLEAN",
            "BOTH",
            "BREADTH",
            "BUCKET",
            "BULK",
            "BY",
            "BYTE",
            "CALL",
            "CALLED",
            "CALLING",
            "CAPACITY",
            "CASCADE",
            "CASCADED",
            "CASE",
            "CAST",
            "CATALOG",
            "CHAR",
            "CHARACTER",
            "CHECK",
            "CLASS",
            "CLOB",
            "CLOSE",
            "CLUSTER",
            "CLUSTERED",
            "CLUSTERING",
            "CLUSTERS",
            "COALESCE",
            "COLLATE",
            "COLLATION",
            "COLLECTION",
            "COLUMN",
            "COLUMNS",
            "COMBINE",
            "COMMENT",
            "COMMIT",
            "COMPACT",
            "COMPILE",
            "COMPRESS",
            "CONDITION",
            "CONFLICT",
            "CONNECT",
            "CONNECTION",
            "CONSISTENCY",
            "CONSISTENT",
            "CONSTRAINT",
            "CONSTRAINTS",
            "CONSTRUCTOR",
            "CONSUMED",
            "CONTINUE",
            "CONVERT",
            "COPY",
            "CORRESPONDING",
            "COUNT",
            "COUNTER",
            "CREATE",
            "CROSS",
            "CUBE",
            "CURRENT",
            "CURSOR",
            "CYCLE",
            "DATA",
            "DATABASE",
            "DATE",
            "DATETIME",
            "DAY",
            "DEALLOCATE",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DEFERRABLE",
            "DEFERRED",
            "DEFINE",
            "DEFINED",
            "DEFINITION",
            "DELETE",
            "DELIMITED",
            "DEPTH",
            "DEREF",
            "DESC",
            "DESCRIBE",
            "DESCRIPTOR",
            "DETACH",
            "DETERMINISTIC",
            "DIAGNOSTICS",
            "DIRECTORIES",
            "DISABLE",
            "DISCONNECT",
            "DISTINCT",
            "DISTRIBUTE",
            "DO",
            "DOMAIN",
            "DOUBLE",
            "DROP",
            "DUMP",
            "DURATION",
            "DYNAMIC",
            "EACH",
            "ELEMENT",
            "ELSE",
            "ELSEIF",
            "EMPTY",
            "ENABLE",
            "END",
            "EQUAL",
            "EQUALS",
            "ERROR",
            "ESCAPE",
            "ESCAPED",
            "EVAL",
            "EVALUATE",
            "EXCEEDED",
            "EXCEPT",
            "EXCEPTION",
            "EXCEPTIONS",
            "EXCLUSIVE",
            "EXEC",
            "EXECUTE",
            "EXISTS",
            "EXIT",
            "EXPLAIN",
            "EXPLODE",
            "EXPORT",
            "EXPRESSION",
            "EXTENDED",
            "EXTERNAL",
            "EXTRACT",
            "FAIL",
            "FALSE",
            "FAMILY",
            "FETCH",
            "FIELDS",
            "FILE",
            "FILTER",
            "FILTERING",
            "FINAL",
            "FINISH",
            "FIRST",
            "FIXED",
            "FLATTERN",
            "FLOAT",
            "FOR",
            "FORCE",
            "FOREIGN",
            "FORMAT",
            "FORWARD",
            "FOUND",
            "FREE",
            "FROM",
            "FULL",
            "FUNCTION",
            "FUNCTIONS",
            "GENERAL",
            "GENERATE",
            "GET",
            "GLOB",
            "GLOBAL",
            "GO",
            "GOTO",
            "GRANT",
            "GREATER",
            "GROUP",
            "GROUPING",
            "HANDLER",
            "HASH",
            "HAVE",
            "HAVING",
            "HEAP",
            "HIDDEN",
            "HOLD",
            "HOUR",
            "IDENTIFIED",
            "IDENTITY",
            "IF",
            "IGNORE",
            "IMMEDIATE",
            "IMPORT",
            "IN",
            "INCLUDING",
            "INCLUSIVE",
            "INCREMENT",
            "INCREMENTAL",
            "INDEX",
            "INDEXED",
            "INDEXES",
            "INDICATOR",
            "INFINITE",
            "INITIALLY",
            "INLINE",
            "INNER",
            "INNTER",
            "INOUT",
            "INPUT",
            "INSENSITIVE",
            "INSERT",
            "INSTEAD",
            "INT",
            "INTEGER",
            "INTERSECT",
            "INTERVAL",
            "INTO",
            "INVALIDATE",
            "IS",
            "ISOLATION",
            "ITEM",
            "ITEMS",
            "ITERATE",
            "JOIN",
            "KEY",
            "KEYS",
            "LAG",
            "LANGUAGE",
            "LARGE",
            "LAST",
            "LATERAL",
            "LEAD",
            "LEADING",
            "LEAVE",
            "LEFT",
            "LENGTH",
            "LESS",
            "LEVEL",
            "LIKE",
            "LIMIT",
            "LIMITED",
            "LINES",
            "LIST",
            "LOAD",
            "LOCAL",
            "LOCALTIME",
            "LOCALTIMESTAMP",
            "LOCATION",
            "LOCATOR",
            "LOCK",
            "LOCKS",
            "LOG",
            "LOGED",
            "LONG",
            "LOOP",
            "LOWER",
            "MAP",
            "MATCH",
            "MATERIALIZED",
            "MAX",
            "MAXLEN",
            "MEMBER",
            "MERGE",
            "METHOD",
            "METRICS",
            "MIN",
            "MINUS",
            "MINUTE",
            "MISSING",
            "MOD",
            "MODE",
            "MODIFIES",
            "MODIFY",
            "MODULE",
            "MONTH",
            "MULTI",
            "MULTISET",
            "NAME",
            "NAMES",
            "NATIONAL",
            "NATURAL",
            "NCHAR",
            "NCLOB",
            "NEW",
            "NEXT",
            "NO",
            "NONE",
            "NOT",
            "NULL",
            "NULLIF",
            "NUMBER",
            "NUMERIC",
            "OBJECT",
            "OF",
            "OFFLINE",
            "OFFSET",
            "OLD",
            "ON",
            "ONLINE",
            "ONLY",
            "OPAQUE",
            "OPEN",
            "OPERATOR",
            "OPTION",
            "OR",
            "ORDER",
            "ORDINALITY",
            "OTHER",
            "OTHERS",
            "OUT",
            "OUTER",
            "OUTPUT",
            "OVER",
            "OVERLAPS",
            "OVERRIDE",
            "OWNER",
            "PAD",
            "PARALLEL",
            "PARAMETER",
            "PARAMETERS",
            "PARTIAL",
            "PARTITION",
            "PARTITIONED",
            "PARTITIONS",
            "PATH",
            "PERCENT",
            "PERCENTILE",
            "PERMISSION",
            "PERMISSIONS",
            "PIPE",
            "PIPELINED",
            "PLAN",
            "POOL",
            "POSITION",
            "PRECISION",
            "PREPARE",
            "PRESERVE",
            "PRIMARY",
            "PRIOR",
            "PRIVATE",
            "PRIVILEGES",
            "PROCEDURE",
            "PROCESSED",
            "PROJECT",
            "PROJECTION",
            "PROPERTY",
            "PROVISIONING",
            "PUBLIC",
            "PUT",
            "QUERY",
            "QUIT",
            "QUORUM",
            "RAISE",
            "RANDOM",
            "RANGE",
            "RANK",
            "RAW",
            "READ",
            "READS",
            "REAL",
            "REBUILD",
            "RECORD",
            "RECURSIVE",
            "REDUCE",
            "REF",
            "REFERENCE",
            "REFERENCES",
            "REFERENCING",
            "REGEXP",
            "REGION",
            "REINDEX",
            "RELATIVE",
            "RELEASE",
            "REMAINDER",
            "RENAME",
            "REPEAT",
            "REPLACE",
            "REQUEST",
            "RESET",
            "RESIGNAL",
            "RESOURCE",
            "RESPONSE",
            "RESTORE",
            "RESTRICT",
            "RESULT",
            "RETURN",
            "RETURNING",
            "RETURNS",
            "REVERSE",
            "REVOKE",
            "RIGHT",
            "ROLE",
            "ROLES",
            "ROLLBACK",
            "ROLLUP",
            "ROUTINE",
            "ROW",
            "ROWS",
            "RULE",
            "RULES",
            "SAMPLE",
            "SATISFIES",
            "SAVE",
            "SAVEPOINT",
            "SCAN",
            "SCHEMA",
            "SCOPE",
            "SCROLL",
            "SEARCH",
            "SECOND",
            "SECTION",
            "SEGMENT",
            "SEGMENTS",
            "SELECT",
            "SELF",
            "SEMI",
            "SENSITIVE",
            "SEPARATE",
            "SEQUENCE",
            "SERIALIZABLE",
            "SESSION",
            "SET",
            "SETS",
            "SHARD",
            "SHARE",
            "SHARED",
            "SHORT",
            "SHOW",
            "SIGNAL",
            "SIMILAR",
            "SIZE",
            "SKEWED",
            "SMALLINT",
            "SNAPSHOT",
            "SOME",
            "SOURCE",
            "SPACE",
            "SPACES",
            "SPARSE",
            "SPECIFIC",
            "SPECIFICTYPE",
            "SPLIT",
            "SQL",
            "SQLCODE",
            "SQLERROR",
            "SQLEXCEPTION",
            "SQLSTATE",
            "SQLWARNING",
            "START",
            "STATE",
            "STATIC",
            "STATUS",
            "STORAGE",
            "STORE",
            "STORED",
            "STREAM",
            "STRING",
            "STRUCT",
            "STYLE",
            "SUB",
            "SUBMULTISET",
            "SUBPARTITION",
            "SUBSTRING",
            "SUBTYPE",
            "SUM",
            "SUPER",
            "SYMMETRIC",
            "SYNONYM",
            "SYSTEM",
            "TABLE",
            "TABLESAMPLE",
            "TEMP",
            "TEMPORARY",
            "TERMINATED",
            "TEXT",
            "THAN",
            "THEN",
            "THROUGHPUT",
            "TIME",
            "TIMESTAMP",
            "TIMEZONE",
            "TINYINT",
            "TO",
            "TOKEN",
            "TOTAL",
            "TOUCH",
            "TRAILING",
            "TRANSACTION",
            "TRANSFORM",
            "TRANSLATE",
            "TRANSLATION",
            "TREAT",
            "TRIGGER",
            "TRIM",
            "TRUE",
            "TRUNCATE",
            "TTL",
            "TUPLE",
            "TYPE",
            "UNDER",
            "UNDO",
            "UNION",
            "UNIQUE",
            "UNIT",
            "UNKNOWN",
            "UNLOGGED",
            "UNNEST",
            "UNPROCESSED",
            "UNSIGNED",
            "UNTIL",
            "UPDATE",
            "UPPER",
            "URL",
            "USAGE",
            "USE",
            "USER",
            "USERS",
            "USING",
            "UUID",
            "VACUUM",
            "VALUE",
            "VALUED",
            "VALUES",
            "VARCHAR",
            "VARIABLE",
            "VARIANCE",
            "VARINT",
            "VARYING",
            "VIEW",
            "VIEWS",
            "VIRTUAL",
            "VOID",
            "WAIT",
            "WHEN",
            "WHENEVER",
            "WHERE",
            "WHILE",
            "WINDOW",
            "WITH",
            "WITHIN",
            "WITHOUT",
            "WORK",
            "WRAPPED",
            "WRITE",
            "YEAR",
            "ZONE",
        };
    }

    public class DynamoConverters
    {
        public static Func<Type, string> FieldTypeFn { get; set; }
        public static Func<object, DynamoMetadataType, Dictionary<string, AttributeValue>> ToAttributeValuesFn { get; set; }
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

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDictionary<,>)))
                return DynamoType.Map;

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
                return DynamoType.List;

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

            if (props.Length == 0)
                return;

            hash = GetHashKey(props);
            range = props.FirstOrDefault(x => x.HasAttribute<DynamoDBRangeKeyAttribute>())
                 ?? props.FirstOrDefault(x => x.HasAttribute<RangeKeyAttribute>())
                 ?? props.FirstOrDefault(x => x.Name == DynamoProperty.RangeKey);

            //If there's only a single FK attribute that's not overridden by specific Hash or Range attrs
            //Set the hash key as the FK to keep related records in the same hash and 
            //Set the range key as the PK to uniquely defined the record
            var referenceAttrProps = props.Where(x => x.HasAttribute<ReferencesAttribute>()).ToList();
            if (hash == null && range == null && referenceAttrProps.Count == 1)
            {
                hash = referenceAttrProps[0];
                range = GetPrimaryKey(props) ?? props[0];
            }
            else if (hash == null)
            {
                var compositeIndex = type.FirstAttribute<CompositeIndexAttribute>();
                if (compositeIndex != null && compositeIndex.FieldNames.Count > 0)
                {
                    if (compositeIndex.FieldNames.Count > 2)
                        throw new ArgumentException("Only max of 2 fields allowed in [CompositeIndex] for defining Hash and Range Key");

                    var hashField = compositeIndex.FieldNames[0];
                    hash = props.FirstOrDefault(x => x.Name == hashField);
                    if (hash == null)
                        throw new ArgumentException("Could not find Hash Key field '{0}' in CompositeIndex".Fmt(hashField));

                    if (compositeIndex.FieldNames.Count == 2)
                    {
                        var rangeField = compositeIndex.FieldNames[1];
                        range = props.FirstOrDefault(x => x.Name == rangeField);
                        if (range == null)
                            throw new ArgumentException("Could not find Range Key field '{0}' in CompositeIndex".Fmt(rangeField));
                    }
                }
                else
                {
                    //Otherwise set the Id as the hash key if hash key is not explicitly defined
                    hash = GetPrimaryKey(props) ?? props[0];
                }
            }
        }

        private static PropertyInfo GetHashKey(PropertyInfo[] props)
        {
            return props.FirstOrDefault(x => x.HasAttribute<DynamoDBHashKeyAttribute>())
                   ?? props.FirstOrDefault(x => x.HasAttribute<HashKeyAttribute>())
                   ?? props.FirstOrDefault(x => x.Name == DynamoProperty.HashKey);
        }

        private static PropertyInfo GetPrimaryKey(PropertyInfo[] props)
        {
            return props.FirstOrDefault(x =>
                    x.HasAttribute<PrimaryKeyAttribute>() ||
                    x.HasAttribute<AutoIncrementAttribute>())
                ?? props.FirstOrDefault(x => x.Name.EqualsIgnoreCase(IdUtils.IdField));
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataField field, object hash)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { field.Name, ToAttributeValue(db, field.Type, field.DbType, hash) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataType table, DynamoId id)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { table.HashKey.Name, ToAttributeValue(db, table.HashKey.Type, table.HashKey.DbType, id.Hash) },
                    { table.RangeKey.Name, ToAttributeValue(db, table.RangeKey.Type, table.RangeKey.DbType, id.Range) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataType table, object hash, object range)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { table.HashKey.Name, ToAttributeValue(db, table.HashKey.Type, table.HashKey.DbType, hash) },
                    { table.RangeKey.Name, ToAttributeValue(db, table.RangeKey.Type, table.RangeKey.DbType, range) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeValues(IPocoDynamo db, object instance, DynamoMetadataType table)
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

                    value = ApplyFieldBehavior(db, table, field, instance, value);

                    to[field.Name] = ToAttributeValue(db, field.Type, field.DbType, value);
                }

                return to;
            }
        }

        private static object ApplyFieldBehavior(IPocoDynamo db, DynamoMetadataType type, DynamoMetadataField field, object instance, object value)
        {
            if (type == null || field == null || !field.IsAutoIncrement)
                return value;

            var needsId = IsNumberDefault(value);
            if (!needsId)
                return value;

            var nextId = db.Sequences.Increment(type.Name);
            return field.SetValue(instance, nextId);
        }

        private static bool IsNumberDefault(object value)
        {
            return value == null || 0 == (long)Convert.ChangeType(value, typeof(long));
        }

        public virtual AttributeValue ToAttributeValue(IPocoDynamo db, Type fieldType, string dbType, object value)
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
                    return ToNumberSetAttributeValue(value);
                case DynamoType.StringSet:
                    return ToStringSetAttributeValue(value);
                case DynamoType.List:
                    return ToListAttributeValue(db, value);
                case DynamoType.Map:
                    return ToMapAttributeValue(db, value);
                default:
                    return new AttributeValue { S = value.ToJsv() };
            }
        }

        public virtual AttributeValue ToNumberSetAttributeValue(object value)
        {
            var to = new AttributeValue { NS = value.ConvertTo<List<string>>() };
            //DynamoDB does not support empty sets
            //http://docs.amazonaws.cn/en_us/amazondynamodb/latest/developerguide/DataModel.html
            if (to.NS.Count == 0)
                to.NULL = true;
            return to;
        }

        public virtual AttributeValue ToStringSetAttributeValue(object value)
        {
            var to = new AttributeValue { SS = value.ConvertTo<List<string>>() };
            //DynamoDB does not support empty sets
            //http://docs.amazonaws.cn/en_us/amazondynamodb/latest/developerguide/DataModel.html
            if (to.SS.Count == 0)
                to.NULL = true;
            return to;
        }

        public virtual AttributeValue ToMapAttributeValue(IPocoDynamo db, object oMap)
        {
            var map = oMap as IDictionary
                ?? oMap.ToObjectDictionary();

            var meta = DynamoMetadata.GetType(oMap.GetType());

            var to = new Dictionary<string, AttributeValue>();
            foreach (var key in map.Keys)
            {
                var value = map[key];
                if (value != null)
                {
                    value = ApplyFieldBehavior(db,
                        meta,
                        meta != null ? meta.GetField((string)key) : null,
                        oMap,
                        value);
                }

                to[key.ToString()] = value != null
                    ? ToAttributeValue(db, value.GetType(), GetFieldType(value.GetType()), value)
                    : new AttributeValue { NULL = true };
            }
            return new AttributeValue { M = to, IsMSet = true };
        }

        public virtual object FromMapAttributeValue(Dictionary<string, AttributeValue> map, Type type)
        {
            var from = new Dictionary<string, object>();

            var metaType = DynamoMetadata.GetType(type);
            if (metaType == null)
            {
                var toMap = (IDictionary)type.CreateInstance();
                var genericDict = type.GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
                if (genericDict != null)
                {
                    var genericArgs = genericDict.GetGenericArguments();
                    var keyType = genericArgs[1];
                    var valueType = genericArgs[1];

                    foreach (var entry in map)
                    {
                        var key = ConvertValue(entry.Key, keyType);
                        toMap[key] = FromAttributeValue(entry.Value, valueType);
                    }

                    return toMap;
                }

                throw new ArgumentException("Unknown Map Type " + type.Name);
            }

            foreach (var field in metaType.Fields)
            {
                AttributeValue attrValue;
                if (!map.TryGetValue(field.Name, out attrValue))
                    continue;

                from[field.Name] = FromAttributeValue(attrValue, field.Type);
            }

            var to = from.FromObjectDictionary(type);
            return to;
        }

        public virtual AttributeValue ToListAttributeValue(IPocoDynamo db, object oList)
        {
            var list = ((IEnumerable)oList).Map(x => x);
            if (list.Count <= 0)
                return new AttributeValue { L = new List<AttributeValue>(), IsLSet = true };

            var elType = list[0].GetType();
            var elMeta = DynamoMetadata.GetType(elType);
            if (elMeta != null)
            {
                var autoIncrFields = elMeta.Fields.Where(x => x.IsAutoIncrement).ToList();
                foreach (var field in autoIncrFields)
                {
                    //Avoid N+1 by fetching a batch of ids
                    var autoIds = db.Sequences.GetNextSequences(elMeta, list.Count);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var instance = list[i];
                        var value = field.GetValue(instance);
                        if (IsNumberDefault(value))
                            field.SetValue(instance, autoIds[i]);
                    }
                }
            }

            var values = list.Map(x => ToAttributeValue(db, x.GetType(), GetFieldType(x.GetType()), x));
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

        public virtual T FromAttributeValues<T>(DynamoMetadataType table, Dictionary<string, AttributeValue> attributeValues)
        {
            var to = typeof(T).CreateInstance<T>();
            return PopulateFromAttributeValues(to, table, attributeValues);
        }

        public virtual T PopulateFromAttributeValues<T>(T to, DynamoMetadataType table, Dictionary<string, AttributeValue> attributeValues)
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
            if (attr.SS.Count > 0)
                return attr.SS;
            if (attr.NS.Count > 0)
                return attr.NS;
            if (attr.BS.Count > 0)
                return attr.BS;

            return null;
        }

    }

}
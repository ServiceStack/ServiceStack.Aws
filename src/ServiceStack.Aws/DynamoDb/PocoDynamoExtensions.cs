using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace ServiceStack.Aws.DynamoDb
{
    public static class PocoDynamoExtensions
    {
        public static DynamoMetadataTable RegisterTable<T>(this IPocoDynamo db)
        {
            return DynamoMetadata.RegisterTable(typeof(T));
        }

        public static DynamoMetadataTable RegisterTable(this IPocoDynamo db, Type type)
        {
            return DynamoMetadata.RegisterTable(type);
        }

        public static List<DynamoMetadataTable> RegisterTables(this IPocoDynamo db, IEnumerable<Type> types)
        {
            return DynamoMetadata.RegisterTables(types);
        }

        public static void AddValueConverter(this IPocoDynamo db, Type type, IAttributeValueConverter valueConverter)
        {
            DynamoMetadata.Converters.ValueConverters[type] = valueConverter;
        }

        public static Table GetTableSchema<T>(this IPocoDynamo db)
        {
            return db.GetTableSchema(typeof(T));
        }

        public static DynamoMetadataTable GetTableMetadata<T>(this IPocoDynamo db)
        {
            return db.GetTableMetadata(typeof(T));
        }

        public static bool CreateMissingTable<T>(this IPocoDynamo db)
        {
            var table = db.GetTableMetadata<T>();
            return db.CreateMissingTables(new[] { table });
        }

        public static bool CreateMissingTable(this IPocoDynamo db, DynamoMetadataTable table)
        {
            return db.CreateMissingTables(new[] { table });
        }

        public static bool DeleteTable<T>(this IPocoDynamo db, TimeSpan? timeout = null)
        {
            var table = db.GetTableMetadata<T>();
            return db.DeleteTables(new[] { table.Name }, timeout);
        }

        public static ReturnValue ToReturnValue(this ReturnItem returnItem)
        {
            return returnItem == ReturnItem.New
                ? ReturnValue.ALL_NEW
                : returnItem == ReturnItem.Old
                    ? ReturnValue.ALL_OLD
                    : ReturnValue.NONE;
        }
    }
}
using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Logging;

namespace ServiceStack.Aws
{
    public interface IPocoDynamo : IDisposable
    {
        IAmazonDynamoDB DynamoDb { get; }
        List<string> GetTableNames();
        bool CreateNonExistingTables(List<DynamoMetadataTable> tables, TimeSpan? timeout = null);
        bool DeleteAllTables(TimeSpan? timeout = null);
        bool DeleteTables(List<string> tableNames, TimeSpan? timeout = null);
        T GetItemById<T>(object hash);
        PutItemResponse PutItem<T>(T value);
        DeleteItemResponse DeleteItemById<T>(object hash);
        bool WaitForTablesToBeReady(List<string> tableNames, TimeSpan? timeout = null);
        bool WaitForTablesToBeRemoved(List<string> tableNames, TimeSpan? timeout = null);
    }

    public partial class PocoDynamo : IPocoDynamo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PocoDynamo));

        public IAmazonDynamoDB DynamoDb { get; private set; }

        public bool ConsistentRead { get; set; }
        public long ReadCapacityUnits { get; set; }
        public long WriteCapacityUnits { get; set; }

        public HashSet<string> RetryOnErrorCodes { get; set; } 

        public TimeSpan PollTableStatus { get; set; }

        public TimeSpan MaxRetryOnExceptionTimeout { get; set; }

        public PocoDynamo(IAmazonDynamoDB dynamoDb)
        {
            this.DynamoDb = dynamoDb;
            PollTableStatus = TimeSpan.FromSeconds(2);
            MaxRetryOnExceptionTimeout = TimeSpan.FromSeconds(60);
            ReadCapacityUnits = 10;
            WriteCapacityUnits = 5;
            ConsistentRead = true;
            RetryOnErrorCodes = new HashSet<string> {
                "ThrottlingException",
                "ProvisionedThroughputExceededException",
                "LimitExceededException",
                "ResourceInUseException",
            };
        }

        public List<string> GetTableNames()
        {
            return Exec(() => DynamoDb.ListTables().TableNames);
        }

        public bool CreateNonExistingTables(List<DynamoMetadataTable> tables, TimeSpan? timeout = null)
        {
            var existingTableNames = GetTableNames();

            foreach (var table in tables)
            {
                if (existingTableNames.Contains(table.Name))
                    continue;

                var request = ToCreateTableRequest(table);
                Exec(() => DynamoDb.CreateTable(request));
            }

            return WaitForTablesToBeReady(tables.Map(x => x.Name), timeout);
        }

        protected virtual CreateTableRequest ToCreateTableRequest(DynamoMetadataTable table)
        {
            var props = table.Type.GetSerializableProperties();
            if (props.Length == 0)
                throw new NotSupportedException("{0} does not have any serializable properties".Fmt(table.Name));

            var keySchema = new List<KeySchemaElement> {
                new KeySchemaElement(table.HashKey.Name, KeyType.HASH),
            };
            var attrDefinitions = new List<AttributeDefinition> {
                new AttributeDefinition(table.HashKey.Name, table.HashKey.DbType),
            };
            if (table.RangeKey != null)
            {
                keySchema.Add(new KeySchemaElement(table.RangeKey.Name, KeyType.RANGE));
                attrDefinitions.Add(new AttributeDefinition(table.RangeKey.Name, table.RangeKey.DbType));
            }

            var to = new CreateTableRequest
            {
                TableName = table.Name,
                KeySchema = keySchema,
                AttributeDefinitions = attrDefinitions,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = ReadCapacityUnits,
                    WriteCapacityUnits = WriteCapacityUnits,
                }
            };

            return to;
        }

        public bool DeleteAllTables(TimeSpan? timeout = null)
        {
            return DeleteTables(GetTableNames(), timeout);
        }

        public bool DeleteTables(List<string> tableNames, TimeSpan? timeout = null)
        {
            foreach (var tableName in tableNames)
            {
                Exec(() => DynamoDb.DeleteTable(new DeleteTableRequest(tableName)));
            }

            return WaitForTablesToBeRemoved(tableNames);
        }

        public T GetItemById<T>(object hash)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest {
                TableName = table.Name,
                Key = DynamoMetadata.Converters.ToAttributeKeyValue(table.HashKey, hash),
                ConsistentRead = ConsistentRead,
            };

            var response = Exec(() => DynamoDb.GetItem(request));

            if (!response.IsItemSet)
                return default(T);
            var attributeValues = response.Item;

            return DynamoMetadata.Converters.FromAttributeValues<T>(table, attributeValues);
        }

        public PutItemResponse PutItem<T>(T value)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new PutItemRequest
            {
                TableName = table.Name,
                Item = DynamoMetadata.Converters.ToAttributeValues(value, table),
            };

            return Exec(() => DynamoDb.PutItem(request));
        }

        public DeleteItemResponse DeleteItemById<T>(object hash)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = DynamoMetadata.Converters.ToAttributeKeyValue(table.HashKey, hash),
            };

            return Exec(() => DynamoDb.DeleteItem(request));
        }

        public void Dispose()
        {
            if (DynamoDb == null)
                return;

            DynamoDb.Dispose();
            DynamoDb = null;
        }
    }
}
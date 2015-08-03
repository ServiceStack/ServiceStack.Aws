using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Aws
{
    public interface IPocoDynamo : IDisposable
    {
        IAmazonDynamoDB DynamoDb { get; }
        Table GetTableSchema(Type table);
        DynamoMetadataTable GetTableMetadata(Type table);
        List<string> GetTableNames();
        bool CreateMissingTables(IEnumerable<DynamoMetadataTable> tables, TimeSpan? timeout = null);
        bool DeleteAllTables(TimeSpan? timeout = null);
        bool DeleteTables(IEnumerable<string> tableNames, TimeSpan? timeout = null);
        T GetItemById<T>(object id);
        T GetItemByHashAndRange<T>(object hash, object range);
        T PutItem<T>(T value, ReturnItem returnItem = ReturnItem.None);
        T DeleteItemById<T>(object hash, ReturnItem returnItem = ReturnItem.None);
        bool WaitForTablesToBeReady(IEnumerable<string> tableNames, TimeSpan? timeout = null);
        bool WaitForTablesToBeDeleted(IEnumerable<string> tableNames, TimeSpan? timeout = null);
        IPocoDynamo Clone();
    }

    public partial class PocoDynamo : IPocoDynamo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PocoDynamo));

        public IAmazonDynamoDB DynamoDb { get; private set; }

        public bool ConsistentRead { get; set; }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Read Capacity to use
        /// </summary>
        public long ReadCapacityUnits { get; set; }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Write Capacity to use
        /// </summary> 
        public long WriteCapacityUnits { get; set; }

        public HashSet<string> RetryOnErrorCodes { get; set; }

        public TimeSpan PollTableStatus { get; set; }

        public TimeSpan MaxRetryOnExceptionTimeout { get; set; }

        private static DynamoConverters Converters
        {
            get { return DynamoMetadata.Converters; }
        }

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

        public IPocoDynamo Clone()
        {
            return new PocoDynamo(DynamoDb)
            {
                ConsistentRead = ConsistentRead,
                ReadCapacityUnits = ReadCapacityUnits,
                WriteCapacityUnits = WriteCapacityUnits,
                RetryOnErrorCodes = new HashSet<string>(RetryOnErrorCodes),
                PollTableStatus = PollTableStatus,
                MaxRetryOnExceptionTimeout = MaxRetryOnExceptionTimeout,
            };
        }

        public DynamoMetadataTable GetTableMetadata(Type table)
        {
            return DynamoMetadata.GetTable(table);
        }

        public List<string> GetTableNames()
        {
            return Exec(() => DynamoDb.ListTables().TableNames);
        }

        readonly Type[] throwNotFoundExceptions = {
            typeof(ResourceNotFoundException)
        };

        public Table GetTableSchema(Type type)
        {
            var table = DynamoMetadata.GetTable(type);
            return Exec(() =>
            {
                try
                {
                    Table awsTable;
                    Table.TryLoadTable(DynamoDb, table.Name, out awsTable);
                    return awsTable;
                }
                catch (ResourceNotFoundException)
                {
                    return null;
                }
            }, throwNotFoundExceptions);
        }

        public bool CreateMissingTables(IEnumerable<DynamoMetadataTable> tables, TimeSpan? timeout = null)
        {
            var existingTableNames = GetTableNames();

            foreach (var table in tables)
            {
                if (existingTableNames.Contains(table.Name))
                    continue;

                if (Log.IsDebugEnabled)
                    Log.Debug("Creating Table: " + table.Name);

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

        public bool DeleteTables(IEnumerable<string> tableNames, TimeSpan? timeout = null)
        {
            foreach (var tableName in tableNames)
            {
                Exec(() => DynamoDb.DeleteTable(new DeleteTableRequest(tableName)));
            }

            return WaitForTablesToBeDeleted(tableNames);
        }

        public T GetItemById<T>(object id)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(table.HashKey, id),
                ConsistentRead = ConsistentRead,
            };

            var response = Exec(() => DynamoDb.GetItem(request), throwNotFoundExceptions);

            if (!response.IsItemSet)
                return default(T);
            var attributeValues = response.Item;

            return DynamoMetadata.Converters.FromAttributeValues<T>(table, attributeValues);
        }

        public T GetItemByHashAndRange<T>(object hash, object range)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(table, hash, range),
                ConsistentRead = ConsistentRead,
            };

            var response = Exec(() => DynamoDb.GetItem(request), throwNotFoundExceptions);

            if (!response.IsItemSet)
                return default(T);
            var attributeValues = response.Item;

            return DynamoMetadata.Converters.FromAttributeValues<T>(table, attributeValues);
        }

        public T PutItem<T>(T value, ReturnItem returnItem = ReturnItem.None)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new PutItemRequest
            {
                TableName = table.Name,
                Item = DynamoMetadata.Converters.ToAttributeValues(value, table),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = Exec(() => DynamoDb.PutItem(request));

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public T DeleteItemById<T>(object hash, ReturnItem returnItem = ReturnItem.None)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = DynamoMetadata.Converters.ToAttributeKeyValue(table.HashKey, hash),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = Exec(() => DynamoDb.DeleteItem(request));

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
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
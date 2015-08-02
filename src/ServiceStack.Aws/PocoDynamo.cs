using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Aws
{
    public partial class PocoDynamo : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PocoDynamo));

        public IAmazonDynamoDB DynamoDb { get; private set; }

        public bool ConsistentRead { get; set; }
        public long ReadCapacityUnits { get; set; }
        public long WriteCapacityUnits { get; set; }

        public bool ExecuteBatchesAsynchronously { get; set; }

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

            if (!ExecuteBatchesAsynchronously)
            {
                foreach (var table in tables)
                {
                    if (existingTableNames.Contains(table.Name))
                        continue;

                    var request = ToCreateTableRequest(table);
                    Exec(() => DynamoDb.CreateTable(request));
                }
            }
            else
            {
                Exec(() => {
                    var tasks = tables
                        .Where(x => !existingTableNames.Contains(x.Name))
                        .Map(x => DynamoDb.CreateTableAsync(ToCreateTableRequest(x)) as Task)
                        .ToArray();

                    Task.WaitAll(tasks, timeout.GetValueOrDefault(TimeSpan.MaxValue));
                });
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
            if (!ExecuteBatchesAsynchronously)
            {
                foreach (var tableName in tableNames)
                {
                    Exec(() => DynamoDb.DeleteTable(new DeleteTableRequest(tableName)));
                }
            }
            else
            {
                Exec(() => {
                    var tasks = tableNames
                        .Map(x => DynamoDb.DeleteTableAsync(new DeleteTableRequest(x)) as Task)
                        .ToArray();

                    Task.WaitAll(tasks, timeout.GetValueOrDefault(TimeSpan.MaxValue));
                });
            }

            return WaitForTablesToBeRemoved(tableNames);
        }

        protected virtual GetItemRequest ToGetItemRequest(object hash, DynamoMetadataTable table)
        {
            return new GetItemRequest
            {
                TableName = table.Name,
                Key = DynamoMetadata.Converters.ToAttributeKeyValue(table.HashKey, hash),
                ConsistentRead = ConsistentRead,
            };
        }

        public T GetById<T>(object hash)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = ToGetItemRequest(hash, table);

            var response = Exec(() => DynamoDb.GetItem(request));

            if (!response.IsItemSet)
                return default(T);
            var attributeValues = response.Item;

            var to = typeof(T).CreateInstance<T>();

            return DynamoMetadata.Converters.Populate(to, table, attributeValues);
        }

        protected virtual PutItemRequest ToPutItemRequest(object value, DynamoMetadataTable table)
        {
            var to = new PutItemRequest
            {
                TableName = table.Name,
                Item = DynamoMetadata.Converters.ToAttributeValues(value, table),
            };
            return to;
        }

        public PutItemResponse Put<T>(T value)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = ToPutItemRequest(value, table);

            return Exec(() => DynamoDb.PutItem(request));
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
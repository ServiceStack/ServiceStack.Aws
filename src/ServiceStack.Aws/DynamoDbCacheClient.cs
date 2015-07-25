using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Aws
{
    public class DynamoDbCacheClient : ICacheClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DynamoDbCacheClient));

        private IAmazonDynamoDB client;
        private Table awsCacheTableObj;

        private string cacheTableName;
        private int cacheReadCapacity = 10; // free-tier settings
        private int cacheWriteCapacity = 5; // free-tier settings
        private readonly bool scacheTableCreate;
        private const string KeyName = "urn";
        private const string ValueName = "value";
        private const string ExpiresAtName = "expiresAt";

        /// <summary>
        /// The name of the DynamoDB Table - either make sure it is already created with HashKey string named "urn" or pass true into constructor createTableIfMissing argument
        /// </summary>
        public string CacheTableName
        {
            get { return cacheTableName; }
            set { cacheTableName = value; }
        }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Read Capacity to use
        /// </summary>
        public int CacheReadCapacity
        {
            get { return cacheReadCapacity; }
            set { cacheReadCapacity = value; }
        }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Write Capacity to use
        /// </summary> 
        public int CacheWriteCapacity
        {
            get { return cacheWriteCapacity; }
            set { cacheWriteCapacity = value; }
        }

        public DynamoDbCacheClient(IAmazonDynamoDB client, string cacheTableName = "ICacheClientDynamo",
            int readCapacity = 10, int writeCapacity = 5, bool createTableIfMissing = false)
        {
            this.client = client;
            CacheTableName = cacheTableName;
            CacheReadCapacity = readCapacity;
            CacheWriteCapacity = writeCapacity;
            scacheTableCreate = createTableIfMissing;

            if (string.IsNullOrEmpty(CacheTableName))
                throw new MissingFieldException("DynamoCacheClient", "CacheTableName");

            if (scacheTableCreate)
                CreateDynamoCacheTable();
        }

        /// <summary>
        /// DynamoDbCacheClient constructor
        /// </summary>
        /// <param name="awsAccessKey">AWS Access Key ID</param>
        /// <param name="awsSecretKey">AWS Secret Key ID</param>
        /// <param name="region">AWS Region</param>
        /// <param name="cacheTableName">Name of DynamoDB Table</param>
        /// <param name="readCapacity">Desired DynamoDB Read Capacity</param>
        /// <param name="writeCapacity">Desired DynamoDB Write Capacity</param>
        /// <param name="createTableIfMissing">Pass true if you'd like the client to create the DynamoDB table on startup</param>
        public DynamoDbCacheClient(string awsAccessKey, string awsSecretKey, RegionEndpoint region,
                                   string cacheTableName = "ICacheClientDynamo", int readCapacity = 10,
                                   int writeCapacity = 5, bool createTableIfMissing = false)
            : this(new AmazonDynamoDBClient(awsAccessKey, awsSecretKey, region), cacheTableName, readCapacity, writeCapacity, createTableIfMissing)
        {
        }

        private void CreateDynamoCacheTable()
        {
            Log.InfoFormat("Attempting to load DynamoDB table {0}", cacheTableName);
            if (!Table.TryLoadTable(client, CacheTableName, out awsCacheTableObj))
            {
                Log.InfoFormat("DynamoDB table {0} does not exist, attempting to create", cacheTableName);
                try
                {
                    client.CreateTable(new CreateTableRequest
                    {
                        TableName = CacheTableName,
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement
                            {
                                AttributeName = KeyName, KeyType = KeyType.HASH,
                            }
                        },
                        AttributeDefinitions = new List<AttributeDefinition>()
                        {
                            new AttributeDefinition
                            {
                                AttributeName = KeyName, AttributeType = "S"
                            }
                        },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = CacheReadCapacity,
                            WriteCapacityUnits = CacheWriteCapacity,
                        }
                    });
                    Log.InfoFormat("Successfully created DynamoDB table {0}", cacheTableName);
                    WaitUntilTableReady(cacheTableName);
                }
                catch (Exception)
                {
                    Log.ErrorFormat("Could not create DynamoDB table {0}", cacheTableName);
                    throw;
                }

            }
        }

        private void WaitUntilTableDeleted(string tableName)
        {
            string status;
            DateTime startWaitTime = DateTime.UtcNow;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = client.DescribeTable(new DescribeTableRequest
                    {
                        TableName = tableName
                    });

                    Log.InfoFormat("Table name: {0}, status: {1}",
                                   res.Table.TableName,
                                   res.Table.TableStatus);
                    status = res.Table.TableStatus;
                    if (DateTime.UtcNow.Subtract(startWaitTime).Seconds > 60)
                        throw new Exception("Waiting for too long for DynamoDB table to be deleted, please check your AWS Console");

                }
                catch (ResourceNotFoundException)
                {
                    // When the resource is reported as not found, it's deleted so break out of the loop
                    break;
                }
            } while (status == "DELETING");
        }

        private void WaitUntilTableReady(string tableName)
        {
            string status = null;
            DateTime startWaitTime = DateTime.Now;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = client.DescribeTable(new DescribeTableRequest
                    {
                        TableName = tableName
                    });

                    Log.InfoFormat("Table name: {0}, status: {1}",
                                   res.Table.TableName,
                                   res.Table.TableStatus);
                    status = res.Table.TableStatus;
                    if (DateTime.Now.Subtract(startWaitTime).Seconds > 60)
                        throw new Exception("Waiting for too long for DynamoDB table to be created, please check your AWS Console");
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }

        private bool TryGetValue<T>(string key, out T entry)
        {
            Log.InfoFormat("Attempting cache get of key: {0}", key);
            entry = default(T);
            var itemKey = new Dictionary<string, AttributeValue> { { KeyName, new AttributeValue() { S = key } } };
            var response = client.GetItem(CacheTableName, itemKey);

            if (response.IsItemSet)
            {
                var item = response.Item;
                DateTime expiresAt = DateTime.ParseExact(item[ExpiresAtName].S, "s", CultureInfo.InvariantCulture);
                if (DateTime.UtcNow > expiresAt)
                {
                    Log.InfoFormat("Cache key: {0} has expired, removing from cache!", key);
                    Remove(key);
                    return false;
                }
                string jsonData = item[ValueName].S;
                entry = AwsClientUtils.FromJson<T>(jsonData);
                Log.InfoFormat("Cache hit on key: {0}", key);
                return true;
            }
            return false;
        }

        private bool CacheAdd<T>(string key, T value, DateTime expiresAt)
        {
            T entry;
            if (TryGetValue(key, out entry)) return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheReplace<T>(string key, T value, DateTime expiresAt)
        {
            T entry;
            if (!TryGetValue(key, out entry)) return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheSet<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                client.PutItem(
                    new PutItemRequest
                    {
                        TableName = CacheTableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { KeyName, new AttributeValue { S = key } },
                            { ValueName, new AttributeValue { S = AwsClientUtils.ToJson(value) } },
                            {
                                ExpiresAtName,
                                new AttributeValue {S = expiresAt.ToUniversalTime().ToString("s", CultureInfo.InvariantCulture)}
                            }
                        }
                    }
                    );
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int UpdateCounter(string key, int value)
        {
            var itemKey = new Dictionary<string, AttributeValue> { { KeyName, new AttributeValue() { S = key } } };
            var response = client.UpdateItem(new UpdateItemRequest
            {
                TableName = CacheTableName,
                Key = itemKey,
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                {
                    {
                        ValueName,
                        new AttributeValueUpdate
                        {
                            Action = "ADD",
                            Value = new AttributeValue
                            {
                                N =value.ToString(CultureInfo.InvariantCulture)
                            }
                        }
                    }
                },
                ReturnValues = "ALL_NEW"
            });
            return Convert.ToInt32(response.Attributes[ValueName].N);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheAdd(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return CacheAdd(key, value, expiresAt.ToUniversalTime());
        }

        public bool Add<T>(string key, T value)
        {
            return CacheAdd(key, value, DateTime.MaxValue.ToUniversalTime());
        }

        public long Decrement(string key, uint amount)
        {
            return UpdateCounter(key, (int)-amount);
        }

        /// <summary>
        /// IMPORTANT: This method will delete and re-create the DynamoDB table in order to reduce read/write capacity costs, make sure the proper table name and throughput properties are set!
        /// TODO: This method may take upwards of a minute to complete, need to look into a faster implementation
        /// </summary>
        public void FlushAll()
        {
            // Is this the cheapest method per AWS pricing to clear a table? (instead of table scan / remove each item) ??
            client.DeleteTable(new DeleteTableRequest { TableName = CacheTableName });
            // Scaning the table is limited to 1 MB chunks, with a large cache it could result in many Read requests and many Delete requests occurring very quickly which may tap out 
            // the throughput capacity...
            WaitUntilTableDeleted(cacheTableName);
            CreateDynamoCacheTable();
        }

        public T Get<T>(string key)
        {
            T value;
            return TryGetValue(key, out value) ? value : default(T);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = Get<T>(key);
                valueMap[key] = value;
            }
            return valueMap;
        }

        public long Increment(string key, uint amount)
        {
            return UpdateCounter(key, (int)amount);
        }

        public bool Remove(string key)
        {
            try
            {
                var itemKey = new Dictionary<string, AttributeValue> { { KeyName, new AttributeValue() { S = key } } };
                client.DeleteItem(new DeleteItemRequest
                {
                    TableName = CacheTableName,
                    Key = itemKey
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    Remove(key);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Error trying to remove {0} from the cache", key), ex);
                }
            }
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheReplace(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return CacheReplace(key, value, expiresAt.ToUniversalTime());
        }

        public bool Replace<T>(string key, T value)
        {
            return CacheReplace(key, value, DateTime.MaxValue.ToUniversalTime());
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheSet(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt.ToUniversalTime());
        }

        public bool Set<T>(string key, T value)
        {
            return CacheSet(key, value, DateTime.MaxValue.ToUniversalTime());
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }

        public void Dispose()
        {
            if (client != null)
                client.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Aws
{
    public class DynamoDbCacheClient : ICacheClientExtended, IRequiresSchema, IRemoveByPattern
    {
        public const string IdField = "Id";
        public const string DataField = "Data";

        private static readonly ILog Log = LogManager.GetLogger(typeof(DynamoDbCacheClient));

        private readonly IPocoDynamo db;

        private Table schema;
        private readonly DynamoMetadataTable metadata;

        public DynamoDbCacheClient(IPocoDynamo db)
        {
            this.db = db;
            db.RegisterTable<CacheEntry>();
            metadata = db.GetTableMetadata<CacheEntry>();
        }

        public void InitSchema()
        {
            schema = db.GetTableSchema<CacheEntry>();

            if (schema == null)
            {
                db.CreateMissingTable(metadata);
                schema = db.GetTableSchema<CacheEntry>();
            }
        }

        private T GetValue<T>(string key)
        {
            var entry = db.GetItemById<CacheEntry>(key);
            if (entry == null)
                return default(T);

            if (entry.ExpiryDate != null && DateTime.UtcNow > entry.ExpiryDate.Value.ToUniversalTime())
            {
                Remove(key);
                return default(T);
            }

            return entry.Data.FromJson<T>();
        }

        private bool CacheAdd<T>(string key, T value, DateTime? expiresAt)
        {
            var entry = GetValue<T>(key);
            if (!Equals(entry, default(T)))
                return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheReplace<T>(string key, T value, DateTime? expiresAt)
        {
            var entry = GetValue<T>(key);
            if (Equals(entry, default(T)))
                return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheSet<T>(string key, T value, DateTime? expiresAt)
        {
            var now = DateTime.UtcNow;
            string json = AwsClientUtils.ToScopedJson(value);
            var entry = new CacheEntry
            {
                Id = key,
                Data = json,
                CreatedDate = now,
                ModifiedDate = now,
                ExpiryDate = expiresAt,
            };

            db.PutItem(entry);
            return true;
        }

        private int UpdateCounter(string key, int value)
        {
            var response = db.DynamoDb.UpdateItem(new UpdateItemRequest
            {
                TableName = metadata.Name,
                Key = DynamoMetadata.Converters.ToAttributeKeyValue(metadata.HashKey, key),
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
                    {
                        DataField,
                        new AttributeValueUpdate {
                            Action = AttributeAction.ADD,
                            Value = new AttributeValue { N = value.ToString() }
                        }
                    }
                },
                ReturnValues = ReturnValue.ALL_NEW,
            });
            return Convert.ToInt32(response.Attributes[DataField].N);
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
            return CacheAdd(key, value, null);
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
            db.DeleteTable<CacheEntry>();
            db.CreateMissingTable<CacheEntry>();
        }

        public T Get<T>(string key)
        {
            return GetValue<T>(key);
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
            var existingItem = db.DeleteItemById<CacheEntry>(key, ReturnItem.Old);
            return existingItem != null;
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
                    Log.Error("Error trying to remove {0} from the cache".Fmt(key), ex);
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
            return CacheReplace(key, value, null);
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
            return CacheSet(key, value, null);
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            var entry = db.GetItemById<CacheEntry>(key);
            if (entry == null)
                return null;

            if (entry.ExpiryDate == null)
                return TimeSpan.MaxValue;

            return entry.ExpiryDate - DateTime.UtcNow;
        }

        public void RemoveByPattern(string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                var beginWith = pattern.Substring(0, pattern.Length - 1);
                if (beginWith.Contains("*"))
                    throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

                var request = new ScanRequest
                {
                    Limit = 1000,
                    TableName = metadata.Name,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        { ":pattern", new AttributeValue { S = beginWith } }
                    },
                    FilterExpression = "begins_with(Id,:pattern)"
                };
                var response = db.DynamoDb.Scan(request);

                var idsToRemove = new HashSet<string>();
                foreach (Dictionary<string, AttributeValue> values in response.Items)
                {
                    AttributeValue attrId;
                    values.TryGetValue(IdField, out attrId);

                    if (attrId != null && attrId.S != null)
                        idsToRemove.Add(attrId.S);
                }

                RemoveAll(idsToRemove);
            }
            else
                throw new NotImplementedException("DynamoDb only supports begins_with* patterns");
        }

        public void RemoveByRegex(string regex)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (db != null)
                db.Dispose();
        }
    }

    public class CacheEntry
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}

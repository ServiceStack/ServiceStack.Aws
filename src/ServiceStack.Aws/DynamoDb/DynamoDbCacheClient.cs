using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Aws.DynamoDb
{
    public class DynamoDbCacheClient : ICacheClientExtended, IRequiresSchema, IRemoveByPattern
    {
        public const string IdField = "Id";
        public const string DataField = "Data";

        public int PagingLimit { get; set; }

        private static readonly ILog Log = LogManager.GetLogger(typeof(DynamoDbCacheClient));

        private readonly IPocoDynamo db;

        private Table schema;
        private readonly DynamoMetadataType metadata;

        public DynamoDbCacheClient(IPocoDynamo db)
        {
            this.db = db;
            this.PagingLimit = 1000;
            db.RegisterTable<CacheEntry>();
            metadata = db.GetTableMetadata<CacheEntry>();
        }

        public void InitSchema()
        {
            schema = db.GetTableSchema<CacheEntry>();

            if (schema == null)
            {
                db.CreateTableIfMissing(metadata);
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

            Exception lastEx = null;
            var i = 0;
            var firstAttempt = DateTime.UtcNow;
            while (DateTime.UtcNow - firstAttempt < db.MaxRetryOnExceptionTimeout)
            {
                i++;
                try
                {
                    db.PutItem(entry);
                    return true;
                }
                catch (ResourceNotFoundException ex)
                {
                    lastEx = ex;
                    //Table could temporarily not exist after a FlushAll()
                    AwsClientUtils.SleepBackOffMultiplier(i);
                }
            }

            throw new TimeoutException("Exceeded timeout of {0}".Fmt(db.MaxRetryOnExceptionTimeout), lastEx);
        }

        private int UpdateCounterBy(string key, int amount)
        {
            return (int) db.IncrementById<CacheEntry>(key, DataField, amount);
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
            return UpdateCounterBy(key, (int)-amount);
        }

        /// <summary>
        /// IMPORTANT: This method will delete and re-create the DynamoDB table in order to reduce read/write capacity costs, make sure the proper table name and throughput properties are set!
        /// TODO: This method may take upwards of a minute to complete, need to look into a faster implementation
        /// </summary>
        public void FlushAll()
        {
            db.DeleteTable<CacheEntry>();
            db.CreateTableIfMissing<CacheEntry>();
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
            return UpdateCounterBy(key, (int)amount);
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

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            if (pattern == "*")
            {
                var request = new ScanRequest
                {
                    Limit = PagingLimit,
                    TableName = metadata.Name,
                    AttributesToGet = new List<string> { IdField },
                };
                return db.Scan(request, r => r.ToStrings(IdField));
            }
            if (pattern.EndsWith("*"))
            {
                var beginWith = pattern.Substring(0, pattern.Length - 1);
                if (beginWith.Contains("*"))
                    throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

                var request = new ScanRequest
                {
                    Limit = PagingLimit,
                    TableName = metadata.Name,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        { ":pattern", new AttributeValue { S = beginWith } }
                    },
                    FilterExpression = "begins_with({0},:pattern)".Fmt(IdField)
                };
                return db.Scan(request, r => r.ToStrings(IdField));
            }

            throw new NotImplementedException("DynamoDb only supports begins_with* patterns");
        }

        public void RemoveByPattern(string pattern)
        {
            var idsToRemove = GetKeysByPattern(pattern);
            RemoveAll(idsToRemove);
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

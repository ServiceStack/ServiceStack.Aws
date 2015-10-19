using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.DynamoDb
{
    public static class PocoDynamoExtensions
    {
        public static DynamoMetadataType RegisterTable<T>(this IPocoDynamo db)
        {
            return DynamoMetadata.RegisterTable(typeof(T));
        }

        public static DynamoMetadataType RegisterTable(this IPocoDynamo db, Type tableType)
        {
            return DynamoMetadata.RegisterTable(tableType);
        }

        public static void RegisterTables(this IPocoDynamo db, IEnumerable<Type> tableTypes)
        {
            DynamoMetadata.RegisterTables(tableTypes);
        }

        public static void AddValueConverter(this IPocoDynamo db, Type type, IAttributeValueConverter valueConverter)
        {
            DynamoMetadata.Converters.ValueConverters[type] = valueConverter;
        }

        public static Table GetTableSchema<T>(this IPocoDynamo db)
        {
            return db.GetTableSchema(typeof(T));
        }

        public static DynamoMetadataType GetTableMetadata<T>(this IPocoDynamo db)
        {
            return db.GetTableMetadata(typeof(T));
        }

        public static bool CreateTableIfMissing<T>(this IPocoDynamo db)
        {
            var table = db.GetTableMetadata<T>();
            return db.CreateMissingTables(new[] { table });
        }

        public static bool CreateTableIfMissing(this IPocoDynamo db, DynamoMetadataType table)
        {
            return db.CreateMissingTables(new[] { table });
        }

        public static bool DeleteTable<T>(this IPocoDynamo db, TimeSpan? timeout = null)
        {
            var table = db.GetTableMetadata<T>();
            return db.DeleteTables(new[] { table.Name }, timeout);
        }

        public static long DecrementById<T>(this IPocoDynamo db, object id, string fieldName, long amount = 1)
        {
            return db.Increment<T>(id, fieldName, amount * -1);
        }

        public static long IncrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.Increment<T>(id, AwsClientUtils.GetMemberName(fieldExpr), amount);
        }

        public static long DecrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.Increment<T>(id, AwsClientUtils.GetMemberName(fieldExpr), amount * -1);
        }

        public static List<T> GetAll<T>(this IPocoDynamo db)
        {
            return db.ScanAll<T>().ToList();
        }

        public static T GetItem<T>(this IPocoDynamo db, DynamoId id)
        {
            return db.GetItem<T>(id.Hash, id.Range);
        }

        public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<int> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<long> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<string> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
        }

        public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<int> ids)
        {
            db.DeleteItems<T>(ids.Map(x => (object)x));
        }

        public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<long> ids)
        {
            db.DeleteItems<T>(ids.Map(x => (object)x));
        }

        public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<string> ids)
        {
            db.DeleteItems<T>(ids.Map(x => (object)x));
        }

        public static IEnumerable<T> Scan<T>(this IPocoDynamo db, Func<ScanExpression<T>, ScanExpression<T>> fn)
        {
            return db.Scan(fn(db.FromScan<T>()));
        }

        public static List<T> Scan<T>(this IPocoDynamo db, Func<ScanExpression<T>, ScanExpression<T>> fn, int limit)
        {
            return db.Scan(fn(db.FromScan<T>()), limit: limit);
        }

        public static IEnumerable<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request)
        {
            return db.Scan<T>(request.SelectInto<T>());
        }

        public static List<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request, int limit)
        {
            return db.Scan<T>(request.SelectInto<T>(), limit:limit);
        }

        public static IEnumerable<T> Query<T>(this IPocoDynamo db, Func<QueryExpression<T>, QueryExpression<T>> fn)
        {
            return db.Query(fn(db.FromQuery<T>()));
        }

        public static List<T> Query<T>(this IPocoDynamo db, Func<QueryExpression<T>, QueryExpression<T>> fn, int limit)
        {
            return db.Query(fn(db.FromQuery<T>()), limit: limit);
        }

        public static IEnumerable<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request)
        {
            return db.Query<T>(request.SelectInto<T>());
        }

        public static List<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request, int limit)
        {
            return db.Query<T>(request.SelectInto<T>(), limit: limit);
        }

        static AttributeValue NullValue = new AttributeValue { NULL = true };

        public static Dictionary<string, AttributeValue> ToExpressionAttributeValues(this IPocoDynamo db, Dictionary<string, object> args)
        {
            var attrValues = new Dictionary<string, AttributeValue>();
            foreach (var arg in args)
            {
                var key = arg.Key.StartsWith(":")
                    ? arg.Key
                    : ":" + arg.Key;

                if (arg.Value != null)
                {
                    var argType = arg.Value.GetType();
                    var dbType = db.Converters.GetFieldType(argType);

                    attrValues[key] = db.Converters.ToAttributeValue(db, argType, dbType, arg.Value);
                }
                else
                {
                    attrValues[key] = NullValue;
                }
            }
            return attrValues;
        }
    }
}
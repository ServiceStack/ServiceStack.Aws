using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Amazon.DynamoDBv2;
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
            return db.IncrementById<T>(id, fieldName, amount * -1);
        }

        public static long IncrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.IncrementById<T>(id, AwsClientUtils.GetMemberName(fieldExpr), amount);
        }

        public static long DecrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.IncrementById<T>(id, AwsClientUtils.GetMemberName(fieldExpr), amount * -1);
        }

        public static ReturnValue ToReturnValue(this ReturnItem returnItem)
        {
            return returnItem == ReturnItem.New
                ? ReturnValue.ALL_NEW
                : returnItem == ReturnItem.Old
                    ? ReturnValue.ALL_OLD
                    : ReturnValue.NONE;
        }

        public static HashSet<string> ToStrings(this ScanResponse response, string fieldName)
        {
            var to = new HashSet<string>();
            foreach (Dictionary<string, AttributeValue> values in response.Items)
            {
                AttributeValue attrId;
                values.TryGetValue(fieldName, out attrId);

                if (attrId != null && attrId.S != null)
                    to.Add(attrId.S);
            }
            return to;
        }

        public static T ConvertTo<T>(this DynamoMetadataType table,
            Dictionary<string, AttributeValue> attributeValues)
        {
            return DynamoMetadata.Converters.FromAttributeValues<T>(table, attributeValues);
        }

        public static List<T> ConvertAll<T>(this ScanResponse response)
        {
            return response.Items
                .Select(values => DynamoMetadata.GetType<T>().ConvertTo<T>(values))
                .ToList();
        }

        public static List<T> ConvertAll<T>(this QueryResponse response)
        {
            return response.Items
                .Select(values => DynamoMetadata.GetType<T>().ConvertTo<T>(values))
                .ToList();
        }

        public static List<T> GetAll<T>(this IPocoDynamo db)
        {
            return db.ScanAll<T>().ToList();
        }

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<int> ids)
        {
            return db.GetItemsByIds<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<long> ids)
        {
            return db.GetItemsByIds<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<string> ids)
        {
            return db.GetItemsByIds<T>(ids.Map(x => (object)x));
        }

        public static IEnumerable<T> Scan<T>(this IPocoDynamo db, string filterExpression, object args)
        {
            return db.Scan<T>(filterExpression, args.ToObjectDictionary());
        }

        public static IEnumerable<T> Scan<T>(this IPocoDynamo db, Expression<Func<T, bool>> predicate)
        {
            var q = PocoDynamoExpression.FactoryFn(typeof(T), predicate);
            return db.Scan<T>(q.FilterExpression, q.Params);
        }

        public static List<T> Scan<T>(this IPocoDynamo db, string filterExpression, object args, int limit)
        {
            return db.Scan<T>(filterExpression, args.ToObjectDictionary(), limit: limit);
        }

        public static List<T> Scan<T>(this IPocoDynamo db, Expression<Func<T, bool>> predicate, int limit)
        {
            var q = PocoDynamoExpression.FactoryFn(typeof(T), predicate);
            return db.Scan<T>(q.FilterExpression, q.Params, limit:limit);
        }

        public static IEnumerable<T> QueryRelated<T>(this IPocoDynamo db, object hash, Expression<Func<T, bool>> predicate, Func<T, object> fields)
        {
            return db.QueryRelated(hash, predicate, fields(typeof(T).CreateInstance<T>()).GetType().AllFields());
        }

        public static IEnumerable<T> QueryRelated<T>(this IPocoDynamo db, object hash, Expression<Func<T, bool>> predicate, string[] fields = null)
        {
            var q = PocoDynamoExpression.FactoryFn(typeof(T), predicate);

            var keyExpression = "{0} = :k1".Fmt(q.Table.HashKey.Name);
            q.Params[":k1"] = hash;

            var indexField = q.ReferencedFields.FirstOrDefault(x =>
                x != q.Table.HashKey.Name || x != q.Table.RangeKey.Name);

            var index = q.Table.LocalIndexes.FirstOrDefault(x => x.IndexField == indexField);

            if (index == null)
                throw new ArgumentException("Could not find index for field '{0}'".Fmt(indexField));

            var projectionExpr = fields.IsEmpty()
                ? null
                : string.Join(", ", fields);

            return db.QueryIndex<T>(index.Name, keyExpression, q.FilterExpression, q.Params, projectionExpr);
        }
    }
}
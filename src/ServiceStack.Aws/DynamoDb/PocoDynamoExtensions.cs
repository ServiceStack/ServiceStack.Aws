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

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<int> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<long> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
        }

        public static List<T> GetItemsByIds<T>(this IPocoDynamo db, IEnumerable<string> ids)
        {
            return db.GetItems<T>(ids.Map(x => (object)x));
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

        public static IEnumerable<T> QueryRelated<T>(this IPocoDynamo db, Expression<Func<T, bool>> predicate, Func<T, object> fields)
        {
            return db.QueryRelated(predicate, fields(typeof(T).CreateInstance<T>()).GetType().AllFields());
        }

        public static IEnumerable<T> QueryRelated<T>(this IPocoDynamo db, Expression<Func<T, bool>> predicate, string[] fields = null)
        {
            var q = PocoDynamoExpression.FactoryFn(typeof(T), predicate);

            var hashField = q.ReferencedFields.FirstOrDefault(x => x == q.Table.HashKey.Name);
            if (hashField == null)
                throw new ArgumentException("The Hash Key '{0}' was not referenced in Query on '{1}'"
                    .Fmt(q.Table.HashKey.Name, q.Table.Name));

            var indexField = q.ReferencedFields.FirstOrDefault(x =>
                x != q.Table.HashKey.Name && x != q.Table.RangeKey.Name);

            var index = q.Table.LocalIndexes.FirstOrDefault(x => x.RangeKey != null && x.RangeKey.Name == indexField);

            if (index == null)
                throw new ArgumentException("Could not find index for field '{0}'".Fmt(indexField));

            var projectionExpr = fields == null
                ? null
                : string.Join(", ", fields);

            return db.QueryIndex<T>(q.Table.Name, index.Name, q.FilterExpression, q.Params, projectionExpr);
        }

        public static IEnumerable<T> QueryIndex<T>(this IPocoDynamo db, Expression<Func<T, bool>> keyExpression) 
        {
            var table = typeof(T).GetIndexTable();
            if (table == null)
                throw new ArgumentException("'{0}' is not a valid Index Type".Fmt(typeof(T).Name));

            var index = table.GetIndex(typeof(T));
            if (index == null)
                throw new ArgumentException("Could not find index '{0}' on Table '{1}'".Fmt(typeof(T).Name, table.Name));

            var q = PocoDynamoExpression.FactoryFn(typeof(T), keyExpression);

            return db.QueryIndex<T>(table.Name, index.Name, q.FilterExpression, q.Params);
        }
    }
}
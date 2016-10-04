// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

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
        public static IPocoDynamo RegisterTable<T>(this IPocoDynamo db)
        {
            DynamoMetadata.RegisterTable(typeof(T));
            return db;
        }

        public static IPocoDynamo RegisterTable(this IPocoDynamo db, Type tableType)
        {
            DynamoMetadata.RegisterTable(tableType);
            return db;
        }

        public static IPocoDynamo RegisterTables(this IPocoDynamo db, IEnumerable<Type> tableTypes)
        {
            DynamoMetadata.RegisterTables(tableTypes);
            return db;
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

        public static bool CreateTable<T>(this IPocoDynamo db, TimeSpan? timeout = null)
        {
            var table = db.GetTableMetadata<T>();
            return db.CreateTables(new[] { table }, timeout);
        }

        public static long DecrementById<T>(this IPocoDynamo db, object id, string fieldName, long amount = 1)
        {
            return db.Increment<T>(id, fieldName, amount * -1);
        }

        public static long IncrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.Increment<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount);
        }

        public static long DecrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
        {
            return db.Increment<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount * -1);
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

        public static void UpdateItem<T>(this IPocoDynamo db, object hash, object range = null,
            Expression<Func<T>> put = null,
            Expression<Func<T>> add = null,
            Func<T, object> delete = null)
        {
            db.UpdateItem<T>(new DynamoUpdateItem
            {
                Hash = hash,
                Range = range,
                Put = put.AssignedValues(),
                Add = add.AssignedValues(),
                Delete = delete.ToObjectKeys().ToArraySafe(),
            });
        }

        internal static T[] ToArraySafe<T>(this IEnumerable<T> items)
        {
            return items?.ToArray();
        }

        public static Dictionary<string, object> AssignedValue<T>(this Func<T, object> fn)
        {
            if (fn == null)
                return null;

            var instance = typeof(T).CreateInstance<T>();
            var result = fn(instance);
            if (result == null || result.Equals(result.GetType().GetDefaultValue()))
                throw new ArgumentException("Cannot use Assinged Value Expression on null or default values");

            foreach (var entry in instance.ToObjectDictionary())
            {
                if (result.Equals(entry.Value))
                    return new Dictionary<string, object> { { entry.Key, entry.Value } };
            }

            throw new ArgumentException("Could not find AssignedValue");
        }

        public static IEnumerable<string> ToObjectKeys<T>(this Func<T, object> fn)
        {
            if (fn == null)
                return null;

            var instance = typeof(T).CreateInstance<T>();
            var result = fn(instance);

            return result.ToObjectDictionary().Keys;
        }

        public static IEnumerable<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request)
        {
            return db.Scan<T>(request.Projection<T>());
        }

        public static List<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request, int limit)
        {
            return db.Scan<T>(request.Projection<T>(), limit: limit);
        }

        public static IEnumerable<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request)
        {
            return db.Query<T>(request.Projection<T>());
        }

        public static List<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request, int limit)
        {
            return db.Query<T>(request.Projection<T>(), limit: limit);
        }

        static readonly AttributeValue NullValue = new AttributeValue { NULL = true };

        public static Dictionary<string, AttributeValue> ToExpressionAttributeValues(this IPocoDynamo db, Dictionary<string, object> args)
        {
            var attrValues = new Dictionary<string, AttributeValue>();
            foreach (var arg in args)
            {
                var key = arg.Key.StartsWith(":")
                    ? arg.Key
                    : ":" + arg.Key;

                attrValues[key] = ToAttributeValue(db, arg.Value);
            }
            return attrValues;
        }

        internal static AttributeValue ToAttributeValue(this IPocoDynamo db, object value)
        {
            if (value == null)
                return NullValue;

            var argType = value.GetType();
            var dbType = db.Converters.GetFieldType(argType);

            return db.Converters.ToAttributeValue(db, argType, dbType, value);
        }
    }
}
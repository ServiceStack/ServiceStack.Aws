using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb
{
    public class QueryExpression<T> : QueryRequest
    {
        public IPocoDynamo Db { get; private set; }

        public DynamoMetadataType Table { get; private set; }

        public QueryExpression(IPocoDynamo db)
            : this(db, db.GetTableMetadata(typeof(T))) {}

        public QueryExpression(IPocoDynamo db, DynamoMetadataType table)
        {
            this.Db = db;
            this.Table = table;
            this.TableName = this.Table.Name;
        }

        public QueryExpression<T> Clone()
        {
            var q = new QueryExpression<T>(Db)
            {
                Table = Table,
                TableName = TableName,
                AttributesToGet = AttributesToGet,
                ConditionalOperator = ConditionalOperator,
                ConsistentRead = ConsistentRead,
                ExclusiveStartKey = ExclusiveStartKey,
                ExpressionAttributeNames = ExpressionAttributeNames,
                ExpressionAttributeValues = ExpressionAttributeValues,
                FilterExpression = FilterExpression,
                IndexName = IndexName,
                KeyConditionExpression = KeyConditionExpression,
                KeyConditions = KeyConditions,
                Limit = Limit,
                ProjectionExpression = ProjectionExpression,
                QueryFilter = QueryFilter,
                ReturnConsumedCapacity = ReturnConsumedCapacity,
                ScanIndexForward = ScanIndexForward,                
            }.SetSelect(base.Select);

            if (ReadWriteTimeoutInternal != null)
                q.ReadWriteTimeoutInternal = ReadWriteTimeoutInternal;
            if (TimeoutInternal != null)
                q.TimeoutInternal = TimeoutInternal;

            return q;
        }

        internal QueryExpression<T> SetSelect(Select select)
        {
            base.Select = select;
            return this;
        }

        public QueryExpression<T> AddKeyCondition(string keyCondition)
        {
            if (this.KeyConditionExpression == null)
                this.KeyConditionExpression = keyCondition;
            else
                this.KeyConditionExpression += " AND " + keyCondition;

            return this;
        }

        public QueryExpression<T> AddFilterExpression(string filterExpression)
        {
            if (this.FilterExpression == null)
                this.FilterExpression = filterExpression;
            else
                this.FilterExpression += " AND " + filterExpression;

            return this;
        }

        public QueryExpression<T> KeyCondition(string filterExpression, Dictionary<string, object> args = null)
        {
            AddKeyCondition(filterExpression);

            if (args != null)
            {
                Db.ToExpressionAttributeValues(args).Each(x =>
                    this.ExpressionAttributeValues[x.Key] = x.Value);
            }

            return this;
        }

        public QueryExpression<T> KeyCondition(string filterExpression, object args)
        {
            return KeyCondition(filterExpression, args.ToObjectDictionary());
        }

        public QueryExpression<T> KeyCondition(Expression<Func<T, bool>> filterExpression)
        {
            var q = PocoDynamoExpression.Create(typeof(T), filterExpression, paramPrefix: "k");
            return KeyCondition(q.FilterExpression, q.Params);
        }

        public QueryExpression<T> IndexCondition(Expression<Func<T, bool>> keyExpression, string indexName = null)
        {
            var q = PocoDynamoExpression.Create(typeof(T), keyExpression, paramPrefix: "i");

            if (q.ReferencedFields.Distinct().Count() != 1)
                throw new ArgumentException("Only 1 Index can be queried per QueryRequest");

            if (indexName == null)
            {
                var indexField = q.ReferencedFields.First();
                var index = q.Table.GetIndexByField(indexField);

                if (index == null)
                    throw new ArgumentException("Could not find index for field '{0}'".Fmt(indexField));

                this.IndexName = index.Name;
            }
            else
            {
                this.IndexName = indexName;
            }

            AddKeyCondition(q.FilterExpression);

            Db.ToExpressionAttributeValues(q.Params).Each(x =>
                this.ExpressionAttributeValues[x.Key] = x.Value);

            return this;
        }

        public QueryExpression<T> Filter(string filterExpression, Dictionary<string, object> args = null)
        {
            AddFilterExpression(filterExpression);

            if (args != null)
            {
                Db.ToExpressionAttributeValues(args).Each(x =>
                    this.ExpressionAttributeValues[x.Key] = x.Value);
            }

            return this;
        }

        public QueryExpression<T> Filter(string filterExpression, object args)
        {
            return Filter(filterExpression, args.ToObjectDictionary());
        }

        public QueryExpression<T> Filter(Expression<Func<T, bool>> filterExpression)
        {
            var q = PocoDynamoExpression.Create(typeof(T), filterExpression, paramPrefix: "p");
            return Filter(q.FilterExpression, q.Params);
        }

        public QueryExpression<T> OrderByAscending()
        {
            this.ScanIndexForward = true;
            return this;
        }

        public QueryExpression<T> OrderByDescending()
        {
            this.ScanIndexForward = false;
            return this;
        }

        public QueryExpression<T> PagingLimit(int limit)
        {
            this.Limit = limit;
            return this;
        }

        public QueryExpression<T> Select(IEnumerable<string> fields)
        {
            var fieldArray = fields.ToArray();
            this.ProjectionExpression = fieldArray.Length > 0
                ? string.Join(", ", fieldArray)
                : null;
            return this;
        }

        public QueryExpression<T> SelectAll()
        {
            return Select(Table.Fields.Map(x => x.Name));
        }

        public QueryExpression<T> Select<TModel>()
        {
            return Select(typeof(TModel).AllFields().Where(Table.HasField));
        }

        public QueryExpression<T> Select(Func<T, object> fields)
        {
            return Select(fields(typeof(T).CreateInstance<T>()).GetType().AllFields());
        }

        public QueryExpression<T> Select<TModel>(Func<T, object> fields)
        {
            return Select(fields(typeof(TModel).CreateInstance<T>()).GetType().AllFields()
                .Where(Table.HasField));
        }

    }
}
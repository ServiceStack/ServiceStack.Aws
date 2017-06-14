// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Aws.DynamoDb
{
    public class Seq
    {
        public string Id { get; set; }
        public long Counter { get; set; }
    }

    public class DynamoDbSequenceSource : ISequenceSource
    {
        private readonly IPocoDynamo db;
        private readonly DynamoMetadataType table;

        public DynamoDbSequenceSource(IPocoDynamo db)
        {
            this.db = db;
            this.table = DynamoMetadata.RegisterTable<Seq>();
        }

        public void InitSchema()
        {
            db.CreateTableIfMissing(table);
        }

        public long Increment(string tableName, long amount = 1)
        {
            var newCounter = db.IncrementById<Seq>(tableName, x => x.Counter, amount);
            return newCounter;
        }

        public void Reset(string tableName, long startingAt = 0)
        {
            db.PutItem(new Seq { Id = tableName, Counter = startingAt });
        }
    }

    public static class SequenceGeneratorExtensions
    {
        public static long Increment(this ISequenceSource seq, DynamoMetadataType meta, int amount = 1)
        {
            return seq.Increment(meta.Name, amount);
        }

        public static long Increment<T>(this ISequenceSource seq, int amount = 1)
        {
            var tableName = DynamoMetadata.GetType<T>().Name;
            return seq.Increment(tableName, amount);
        }

        public static void Reset<T>(this ISequenceSource seq, int startingAt = 0)
        {
            var tableName = DynamoMetadata.GetType<T>().Name;
            seq.Reset(tableName, startingAt);
        }

        public static long Current(this ISequenceSource seq, DynamoMetadataType meta)
        {
            return seq.Increment(meta.Name, 0);
        }

        public static long Current<T>(this ISequenceSource seq)
        {
            var tableName = DynamoMetadata.GetType<T>().Name;
            return seq.Increment(tableName, 0);
        }

        public static long[] GetNextSequences<T>(this ISequenceSource seq, int noOfSequences)
        {
            return GetNextSequences(seq, DynamoMetadata.GetType<T>(), noOfSequences);
        }

        public static long[] GetNextSequences(this ISequenceSource seq, DynamoMetadataType meta, int noOfSequences)
        {
            var newCounter = seq.Increment(meta, noOfSequences);
            var firstId = newCounter - noOfSequences + 1;
            var ids = new long[noOfSequences];
            for (var i = 0; i < noOfSequences; i++)
            {
                ids[i] = firstId + i;
            }
            return ids;
        }
    }
}
using System;

namespace ServiceStack.Aws.DynamoDb
{
    public interface ISequenceSource : IRequiresSchema
    {
        long Increment(string key, int amount = 1);
        void Reset(string key, int startingAt = 0);
    }

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
            this.table = db.RegisterTable<Seq>();
        }

        public void InitSchema()
        {
            db.CreateTableIfMissing(table);
        }

        public long Increment(string tableName, int amount = 1)
        {
            var newCounter = db.IncrementById<Seq>(tableName, x => x.Counter, amount);
            return newCounter;
        }

        public void Reset(string tableName, int startingAt = 0)
        {
            db.PutItem(new Seq { Id = tableName, Counter = startingAt });
        }
    }

    public static class SequenceGeneratorExtensions
    {
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

        public static long[] GetNextSequences<T>(this ISequenceSource seq, int noOfSequences)
        {
            var newCounter = seq.Increment<T>(noOfSequences);
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
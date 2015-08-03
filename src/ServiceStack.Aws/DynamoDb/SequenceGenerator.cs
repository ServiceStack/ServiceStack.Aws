using System;

namespace ServiceStack.Aws.DynamoDb
{
    public interface ISequenceGenerator : IRequiresSchema
    {
        long[] GentNextSequences(int noOfSequences);
    }

    public class Seq
    {
        public string Id { get; set; }
        public long Counter { get; set; }
    }

    public class SequenceGenerator : ISequenceGenerator
    {
        private readonly IPocoDynamo db;
        private readonly DynamoMetadataTable table;

        public SequenceGenerator(IPocoDynamo db)
        {
            this.db = db;
            this.table = db.RegisterTable<Seq>();
        }

        public long[] GentNextSequences(int noOfSequences)
        {
            throw new NotImplementedException();
        }

        public void InitSchema()
        {
            db.CreateTableIfMissing(table);
        }
    }

    public static class SequenceGeneratorExtensions
    {

    }
}
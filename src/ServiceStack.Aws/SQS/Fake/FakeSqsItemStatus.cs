namespace ServiceStack.Aws.SQS.Fake
{
    public enum FakeSqsItemStatus
    {
        Unknown = 0,
        Queued = 1,
        InFlight = 2,
        Deleted = 3
    }
}
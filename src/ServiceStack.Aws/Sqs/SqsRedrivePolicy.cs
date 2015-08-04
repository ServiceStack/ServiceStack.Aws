namespace ServiceStack.Aws.Sqs
{
    public class SqsRedrivePolicy
    {
        public int MaxReceiveCount { get; set; }
        public string DeadLetterTargetArn { get; set; }
    }
}

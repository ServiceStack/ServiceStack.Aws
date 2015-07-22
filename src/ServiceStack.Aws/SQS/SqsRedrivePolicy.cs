namespace ServiceStack.Aws.Sqs
{
    public class SqsRedrivePolicy
    {
        public int maxReceiveCount { get; set; }
        public string deadLetterTargetArn { get; set; }
    }
}

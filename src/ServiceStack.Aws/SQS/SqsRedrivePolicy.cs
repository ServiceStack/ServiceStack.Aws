using System;

namespace ServiceStack.Aws.SQS
{
    public class SqsRedrivePolicy
    {
        public int maxReceiveCount { get; set; }
        public string deadLetterTargetArn { get; set; }
    }
}

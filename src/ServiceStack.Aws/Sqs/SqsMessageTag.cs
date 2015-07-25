using System;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMessageTag
    {
        public string QName { get; set; }
        public string RHandle { get; set; }

        public static string CreateTag(string queueName, string receiptHandle)
        {
            return AwsClientUtils.ToJson(new SqsMessageTag
            {
                QName = queueName,
                RHandle = receiptHandle
            });
        }
    }
}

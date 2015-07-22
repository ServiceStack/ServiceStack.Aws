using System;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMessageTag
    {
        public string QName { get; set; }
        public string RHandle { get; set; }

        public static String CreateTag(string queueName, string receiptHandle)
        {
            return new SqsMessageTag
                   {
                       QName = queueName,
                       RHandle = receiptHandle
                   }.ToJsv();
        }
    }
}

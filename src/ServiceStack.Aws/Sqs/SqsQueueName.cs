using System;
using System.Collections.Concurrent;

namespace ServiceStack.Aws.Sqs
{
    public static class SqsQueueNames
    {
        private static readonly ConcurrentDictionary<string, SqsQueueName> queueNameMap = new ConcurrentDictionary<string, SqsQueueName>();

        public static SqsQueueName GetSqsQueueName(string originalQueueName)
        {
            SqsQueueName sqn;

            if (queueNameMap.TryGetValue(originalQueueName, out sqn))
                return sqn;

            sqn = new SqsQueueName(originalQueueName);

            return queueNameMap.TryAdd(originalQueueName, sqn)
                ? sqn
                : queueNameMap[originalQueueName];
        }
    }

    public class SqsQueueName : IEquatable<SqsQueueName>
    {
        public SqsQueueName(string originalQueueName)
        {
            QueueName = originalQueueName;
            AwsQueueName = originalQueueName.ToValidQueueName();
        }

        public string QueueName { get; }
        public string AwsQueueName { get; private set; }

        public bool Equals(SqsQueueName other)
        {
            return other != null &&
                   QueueName.Equals(other.QueueName, StringComparison.OrdinalIgnoreCase);
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var asQueueName = obj as SqsQueueName;

            return asQueueName != null && Equals((SqsQueueName)obj);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return QueueName;
        }
    }
}
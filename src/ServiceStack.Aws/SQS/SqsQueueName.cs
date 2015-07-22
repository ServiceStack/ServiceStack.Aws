using System;
using System.Collections.Concurrent;

namespace ServiceStack.Aws.Sqs
{
    public static class SqsQueueNames
    {
        private static readonly ConcurrentDictionary<string, SqsQueueName> _queueNameMap = new ConcurrentDictionary<string, SqsQueueName>();

        public static SqsQueueName GetSqsQueueName(string originalQueueName)
        {
            SqsQueueName sqn;

            if (_queueNameMap.TryGetValue(originalQueueName, out sqn))
            {
                return sqn;
            }

            sqn = new SqsQueueName(originalQueueName);

            return _queueNameMap.TryAdd(originalQueueName, sqn)
                       ? sqn
                       : _queueNameMap[originalQueueName];
        }
    }

    public class SqsQueueName : IEquatable<SqsQueueName>
    {
        public SqsQueueName(string originalQueueName)
        {
            QueueName = originalQueueName;
            AwsQueueName = originalQueueName.ToValidQueueName();
        }

        public string QueueName { get; private set; }
        public string AwsQueueName { get; private set; }

        public Boolean Equals(SqsQueueName other)
        {
            return other != null &&
                   QueueName.Equals(other.QueueName, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public override Boolean Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var asQueueName = obj as SqsQueueName;

            return asQueueName != null && Equals((SqsQueueName)obj);
        }

        public override Int32 GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return QueueName;
        }
    }
}
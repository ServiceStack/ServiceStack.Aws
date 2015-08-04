using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Aws.Sqs.Fake
{
    public class FakeSqsQueueItem
    {
        public FakeSqsQueueItem()
        {
            MessageId = Guid.NewGuid().ToString("N");
            ReceiptHandle = Guid.NewGuid().ToString("N");
        }

        public string MessageId { get; private set; }
        public string ReceiptHandle { get; private set; }

        public string Body { get; set; }
        public FakeSqsItemStatus Status { get; set; }
        public long InFlightUntil { get; set; }

        private Dictionary<string, string> attributes = new Dictionary<string, string>();
        public Dictionary<string, string> Attributes
        {
            get { return attributes ?? new Dictionary<string, string>(); }
            set { attributes = value ?? new Dictionary<string, string>(); }
        }

        public FakeSqsItemStatus GetStatus()
        {
            if (Status == FakeSqsItemStatus.InFlight &&
                DateTime.UtcNow.ToUnixTime() >= InFlightUntil)
            {
                Status = FakeSqsItemStatus.Queued;
            }
            return Status;
        }

    }
}
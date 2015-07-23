using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Timers;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.Support;
using Timer = System.Timers.Timer;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBufferFactory : ISqsMqBufferFactory
    {
        private readonly SqsConnectionFactory sqsConnectionFactory;
        private static readonly ConcurrentDictionary<string, ISqsMqBuffer> queueNameBuffers = new ConcurrentDictionary<string, ISqsMqBuffer>();
        private Timer timer;
        private int processingTimer = 0;

        public SqsMqBufferFactory(SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            this.sqsConnectionFactory = sqsConnectionFactory;
        }

        public Action<Exception> ErrorHandler { get; set; }

        private int bufferFlushIntervalSeconds = 0;
        public int BufferFlushIntervalSeconds
        {
            get { return bufferFlushIntervalSeconds; }
            set
            {
                bufferFlushIntervalSeconds = value > 0
                    ? value
                    : 0;

                if (timer != null)
                    return;

                timer = new Timer(bufferFlushIntervalSeconds)
                {
                    AutoReset = false
                };

                timer.Elapsed += OnTimerElapsed;
                timer.Start();
            }
        }

        private void OnTimerElapsed(object source, ElapsedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref processingTimer, 1, 0) > 0)
                return;

            try
            {
                foreach (var buffer in queueNameBuffers)
                {
                    buffer.Value.Drain(fullDrain: false);
                }
            }
            finally
            {
                if (bufferFlushIntervalSeconds <= 0)
                {
                    timer.Stop();
                    timer = null;
                }
                else
                {
                    timer.Interval = bufferFlushIntervalSeconds;
                    timer.Start();
                }

                Interlocked.CompareExchange(ref processingTimer, 0, 1);
            }

        }

        public ISqsMqBuffer GetOrCreate(SqsQueueDefinition queueDefinition)
        {
            var queueName = queueDefinition.QueueName;

            ISqsMqBuffer buffer;

            if (queueNameBuffers.TryGetValue(queueName, out buffer))
                return buffer;

            buffer = queueDefinition.DisableBuffering
                ? (ISqsMqBuffer)new SqsMqBufferNonBuffered(queueDefinition, sqsConnectionFactory)
                : (ISqsMqBuffer)new SqsMqBuffer(queueDefinition, sqsConnectionFactory);

            buffer.ErrorHandler = ErrorHandler;

            queueNameBuffers.TryAdd(queueName, buffer);

            return queueNameBuffers[queueName];
        }

        public void Dispose()
        {
            foreach (var buffer in queueNameBuffers)
            {
                buffer.Value.Dispose();
            }
        }
    }
}
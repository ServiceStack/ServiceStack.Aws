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
        private readonly SqsConnectionFactory _sqsConnectionFactory;
        private static readonly ConcurrentDictionary<string, ISqsMqBuffer> _queueNameBuffers = new ConcurrentDictionary<string, ISqsMqBuffer>();
        private Timer _timer;
        private int _processingTimer = 0;

        public SqsMqBufferFactory(SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            _sqsConnectionFactory = sqsConnectionFactory;
        }

        public Action<Exception> ErrorHandler { get; set; }

        private int _bufferFlushIntervalSeconds = 0;
        public int BufferFlushIntervalSeconds
        {
            get { return _bufferFlushIntervalSeconds; }
            set
            {
                _bufferFlushIntervalSeconds = value > 0
                                                  ? value
                                                  : 0;

                if (_timer != null)
                {
                    return;
                }

                _timer = new Timer(_bufferFlushIntervalSeconds)
                         {
                             AutoReset = false
                         };

                _timer.Elapsed += OnTimerElapsed;
                _timer.Start();
            }
        }

        private void OnTimerElapsed(Object source, ElapsedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _processingTimer, 1, 0) > 0)
            {
                return;
            }

            try
            {
                foreach (var buffer in _queueNameBuffers)
                {
                    buffer.Value.Drain(fullDrain: false);
                }
            }
            finally
            {
                if (_bufferFlushIntervalSeconds <= 0)
                {
                    _timer.Stop();
                    _timer = null;
                }
                else
                {
                    _timer.Interval = _bufferFlushIntervalSeconds;
                    _timer.Start();
                }

                Interlocked.CompareExchange(ref _processingTimer, 0, 1);
            }

        }

        public ISqsMqBuffer GetOrCreate(SqsQueueDefinition queueDefinition)
        {
            var queueName = queueDefinition.QueueName;

            ISqsMqBuffer buffer;

            if (_queueNameBuffers.TryGetValue(queueName, out buffer))
            {
                return buffer;
            }

            buffer = queueDefinition.DisableBuffering
                         ? (ISqsMqBuffer)new SqsMqBufferNonBuffered(queueDefinition, _sqsConnectionFactory)
                         : (ISqsMqBuffer)new SqsMqBuffer(queueDefinition, _sqsConnectionFactory);

            buffer.ErrorHandler = ErrorHandler;

            _queueNameBuffers.TryAdd(queueName, buffer);

            return _queueNameBuffers[queueName];
        }

        public void Dispose()
        {
            foreach (var buffer in _queueNameBuffers)
            {
                buffer.Value.Dispose();
            }
        }
    }
}
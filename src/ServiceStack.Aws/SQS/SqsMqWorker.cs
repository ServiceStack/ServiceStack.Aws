using System;
using System.IO;
using System.Threading;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.SQS
{
    public class SqsMqWorker : IMqWorker<SqsMqWorker>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SqsMqWorker));

        private readonly Object _msgLock = new Object();
        private readonly ISqsMqMessageFactory _mqFactory;
        private readonly SqsMqWorkerInfo _queueWorkerInfo;
        private readonly IMessageHandler _messageHandler;
        private readonly Action<SqsMqWorker, Exception> _errorHandler;

        private IMessageQueueClient _mqClient;
        private Thread _bgThread;
        private int _status;
        private int _totalMessagesProcessed;
        
        public SqsMqWorker(ISqsMqMessageFactory mqFactory,
                           SqsMqWorkerInfo queueWorkerInfo,
                           String queueName,
                           Action<SqsMqWorker, Exception> errorHandler)
        {
            Guard.AgainstNullArgument(mqFactory, "mqFactory");
            Guard.AgainstNullArgument(queueWorkerInfo, "queueWorkerInfo");
            Guard.AgainstNullArgument(queueName, "queueName");
            Guard.AgainstNullArgument(queueWorkerInfo.MessageHandlerFactory, "queueWorkerInfo.MessageHandlerFactory");

            _mqFactory = mqFactory;
            _queueWorkerInfo = queueWorkerInfo;
            _errorHandler = errorHandler;
            _messageHandler = _queueWorkerInfo.MessageHandlerFactory.CreateMessageHandler();
            QueueName = queueName;
        }
        
        public string QueueName { get; set; }

        public int TotalMessagesProcessed
        {
            get { return _totalMessagesProcessed; }
        }

        public IMessageQueueClient MqClient
        {
            get { return _mqClient ?? (_mqClient = _mqFactory.CreateMessageQueueClient()); }
        }

        public SqsMqWorker Clone()
        {
            return new SqsMqWorker(_mqFactory, _queueWorkerInfo, QueueName, _errorHandler);
        }

        public IMessageHandlerStats GetStats()
        {
            return _messageHandler.GetStats();
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Started)
            {
                return;
            }
            
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Disposed)
            {
                throw new ObjectDisposedException("MQ Host has been disposed");
            }

            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Stopping)
            {
                KillBgThreadIfExists();
            }

            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped)
            {
                _log.Debug("Starting MQ Handler Worker: {0}...".Fmt(QueueName));

                //Should only be 1 thread past this point
                _bgThread = new Thread(Run)
                            {
                                Name = "{0}: {1}".Fmt(GetType().Name, QueueName),
                                IsBackground = true,
                            };

                _bgThread.Start();
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Disposed)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                _log.Debug("Stopping SQS MQ Handler Worker: {0}...".Fmt(QueueName));
                
                Thread.Sleep(100);
                
                lock(_msgLock)
                {
                    Monitor.Pulse(_msgLock);
                }
                
                DisposeMqClient();
            }
        }

        private void Run()
        {
            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting)
            {
                return;
            }

            try
            {
                lock(_msgLock)
                {
                    StartPolling();
                }
            }
            catch(ThreadInterruptedException)
            {   // Expected exceptions from Kill()
                _log.Warn("Received ThreadInterruptedException in Worker: {0}".Fmt(QueueName));
            }
            catch(ThreadAbortException)
            {   // Expected exceptions from Kill()
                _log.Warn("Received ThreadAbortException in Worker: {0}".Fmt(QueueName));
            }
            catch (Exception ex)
            {   
                Stop();

                if (_errorHandler != null)
                {
                    _errorHandler(this, ex);
                }
            }
            finally
            {
                try
                {
                    DisposeMqClient();
                }
                catch { }

                // If in an invalid state, Dispose() this worker.
                if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Stopping) != WorkerStatus.Stopping)
                {
                    Dispose();
                }

                _bgThread = null;
            }
        }

        private void StartPolling()
        {
            var retryCount = 0;

            while (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Started)
            {
                try
                {
                    var msgsProcessedThisTime =
                        _messageHandler.ProcessQueue(MqClient,
                                                     QueueName,
                                                     () => Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Started);

                    _totalMessagesProcessed += msgsProcessedThisTime;
                    
                    Monitor.Wait(_msgLock, millisecondsTimeout: 1000);

                    retryCount = 0;
                }
                catch(EndOfStreamException)
                {
                    throw;
                }
                catch (Exception ex)
                {   
                    if (Interlocked.CompareExchange(ref _status, 0, 0) != WorkerStatus.Started)
                    {   // No longer suppossed to be running...
                        return;
                    }

                    _log.Debug("Received exception polling in MqWorker {0}".Fmt(QueueName), ex);

                    // If it was an unexpected exception, pause for a bit before retrying
                    var waitMs = Math.Min(retryCount++ * 2000, 60000);
                    _log.Debug("Retrying poll after {0}ms...".Fmt(waitMs), ex);
                    Thread.Sleep(waitMs);
                }
            }
        }

        private void KillBgThreadIfExists()
        {
            try
            {
                if (_bgThread == null || !_bgThread.IsAlive)
                {
                    return;
                }

                if (!_bgThread.Join(10000))
                {
                    _log.Warn(String.Concat("Interrupting previous Background Worker: ", _bgThread.Name));

                    _bgThread.Interrupt();

                    if (!_bgThread.Join(TimeSpan.FromSeconds(10)))
                    {
                        _log.Warn(String.Concat(_bgThread.Name, " just wont die, so we're now aborting it..."));
                        _bgThread.Abort();
                    }
                }

            }
            finally
            {
                _bgThread = null;
                Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, _status);
            }
        }

        private void DisposeMqClient()
        {
            // Disposing mqClient causes an EndOfStreamException to be thrown in StartSubscription
            if (_mqClient == null)
            {
                return;
            }

            _mqClient.Dispose();
            _mqClient = null;
        }

        public virtual void Dispose()
        {
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Disposed)
            {
                return;
            }

            Stop();

            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Disposed, WorkerStatus.Stopped) != WorkerStatus.Stopped)
            {
                Interlocked.CompareExchange(ref _status, WorkerStatus.Disposed, WorkerStatus.Stopping);
            }

            try
            {
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                _log.Error(String.Concat("Error Disposing MessageHandlerWorker for: ", QueueName), ex);
            }
        }

    }
}
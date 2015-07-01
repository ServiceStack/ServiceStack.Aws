using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.SQS
{
    // The majority of the code here was lifted/massaged from the existing MQ Server implmentations
    public abstract class BaseMqServer<TWorker> : IMessageService
        where TWorker : class, IMqWorker<TWorker>
    {
        protected readonly ILog _log;
        protected readonly String _typeName;
        private readonly object _msgLock = new object();
        private readonly object _statusLock = new object();

        protected int _status;
        protected List<TWorker> _workers;
        private Thread _bgThread;
        private long _bgThreadCount;
        
        private long _timesStarted;
        private long _doOperation = WorkerOperation.NoOp;
        private long _noOfErrors = 0;
        private int _noOfContinuousErrors = 0;
        private string _lastExMsg = null;

        public BaseMqServer()
        {
            var type = GetType();

            _log = LogManager.GetLogger(type);

            _typeName = type.Name;

            this.ErrorHandler = ex => _log.Error(String.Concat("Exception in ", _typeName, " MQ Server: ", ex.Message), ex);
        }

        protected abstract void Init();

        public abstract void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn);

        public abstract void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn,
                                                Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx);

        /// <summary>
        /// The Message Factory used by this MQ Server
        /// </summary>
        public abstract IMessageFactory MessageFactory { get; }

        public IMessageHandlerStats GetStats()
        {
            lock (_workers)
            {
                var total = new MessageHandlerStats("All Handlers");
                _workers.ForEach(x => total.Add(x.GetStats()));
                return total;
            }
        }

        public string GetStatus()
        {
            lock (_statusLock)
            {
                return WorkerStatus.ToString(_status);
            }
        }

        public string GetStatsDescription()
        {
            lock (_workers)
            {
                var sb = new StringBuilder("#MQ SERVER STATS:\n");
                sb.AppendLine("===============");
                sb.AppendLine("Current Status: " + GetStatus());
                sb.AppendLine("Listening On: " + String.Join(", ", _workers.Select(x => x.QueueName).ToArray()));
                sb.AppendLine("Times Started: " + Interlocked.CompareExchange(ref _timesStarted, 0, 0));
                sb.AppendLine("Num of Errors: " + Interlocked.CompareExchange(ref _noOfErrors, 0, 0));
                sb.AppendLine("Num of Continuous Errors: " + Interlocked.CompareExchange(ref _noOfContinuousErrors, 0, 0));
                sb.AppendLine("Last ErrorMsg: " + _lastExMsg);
                sb.AppendLine("===============");
                
                foreach (var worker in _workers)
                {
                    sb.AppendLine(worker.GetStats().ToString());
                    sb.AppendLine("---------------\n");
                }

                return sb.ToString();
            }
        }

        protected void WorkerErrorHandler(TWorker source, Exception ex)
        {
            _log.Error(String.Concat("Received exception in Worker: ", source.QueueName), ex);
            
            var sourceWorker = _workers.SingleOrDefault(w => ReferenceEquals(w, source));

            if (sourceWorker == null)
            {
                return;
            }

            _log.Debug(String.Concat("Starting new ", source.QueueName, " worker..."));

            _workers.Remove(sourceWorker);

            var newWorker = sourceWorker.Clone();
            _workers.Add(newWorker);
            newWorker.Start();

            sourceWorker.Dispose();
            sourceWorker = null;
        }

        /// 
        /// <summary>
        /// Wait before Starting the MQ Server after a restart 
        /// </summary>
        public int? KeepAliveRetryAfterMs { get; set; }

        /// <summary>
        /// Wait (in seconds) before starting the MQ Server after a restart 
        /// </summary>
        public int? WaitBeforeNextRestart { get; set; }
        
        public List<Type> RegisteredTypes { get; private set; }

        public long BgThreadCount
        {
            get { return Interlocked.CompareExchange(ref _bgThreadCount, 0, 0); }
        }

        /// <summary>
        /// Execute global error handler logic. Must be thread-safe.
        /// </summary>
        public Action<Exception> ErrorHandler { get; set; }
        
        /// <summary>
        /// If you only want to enable priority queue handlers (and threads) for specific msg types
        /// </summary>
        public string[] PriortyQueuesWhitelist { get; set; }

        /// <summary>
        /// Don't listen on any Priority Queues
        /// </summary>
        public bool DisablePriorityQueues
        {
            set
            {
                PriortyQueuesWhitelist = new string[0];
            }
        }

        /// <summary>
        /// Opt-in to only publish responses on this white list. 
        /// Publishes all responses by default.
        /// </summary>
        public string[] PublishResponsesWhitelist { get; set; }

        /// <summary>
        /// Don't publish any response messages
        /// </summary>
        public bool DisablePublishingResponses
        {
            set { PublishResponsesWhitelist = value ? new string[0] : null; }
        }

        protected abstract void DoDispose();

        void DisposeWorkerThreads()
        {
            _log.Debug(String.Concat("Disposing all ", _typeName, " MQ Server worker threads..."));

            if (_workers != null)
            {
                _workers.ForEach(w => w.Dispose());
            }
        }

        public void Dispose()
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
                DisposeWorkerThreads();
            }
            catch (Exception ex)
            {
                _log.Error("Error DisposeWorkerThreads(): ", ex);
            }

            try
            {   // Give a small time slice to die gracefully
                Thread.Sleep(100);
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                if (this.ErrorHandler != null)
                {
                    this.ErrorHandler(ex);
                }
            }

            DoDispose();
        }
        
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Started)
            {   // Already started, (re)start workers as needed and done
                StartWorkerThreads();
                return;
            }

            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Disposed)
            {
                throw new ObjectDisposedException(String.Concat(_typeName, " MQ Host has been disposed"));
            }

            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Starting, WorkerStatus.Stopped) != WorkerStatus.Stopped)
            {
                return;
            }

            // 1-thread now from here on
            try
            {
                Init();

                if (_workers == null || _workers.Count == 0)
                {
                    _log.Warn(String.Concat("Cannot start ", _typeName, " MQ Server with no Message Handlers registered, ignoring."));
                    Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Starting);
                    return;
                }

                StartWorkerThreads();

                if (_bgThread != Thread.CurrentThread)
                {
                    KillBgThreadIfExists();

                    _bgThread = new Thread(RunLoop)
                               {
                                   IsBackground = true,
                                   Name = String.Concat(_typeName, " MQ Server ", Interlocked.Increment(ref _bgThreadCount))
                               };

                    _bgThread.Start();

                    _log.Debug(String.Concat("Started Background Thread: ", _bgThread.Name));
                }
                else
                {
                    _log.Debug(String.Concat("Retrying RunLoop() on Thread: ", _bgThread.Name));

                    RunLoop();
                }
            }
            catch (Exception ex)
            {
                if (this.ErrorHandler != null)
                {
                    this.ErrorHandler(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Disposed)
            {
                throw new ObjectDisposedException("MQ Host has been disposed");
            }

            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                lock (_msgLock)
                {
                    Interlocked.CompareExchange(ref _doOperation, WorkerOperation.Stop, _doOperation);
                    Monitor.Pulse(_msgLock);
                }
            }

        }
        
        private void StartWorkerThreads()
        {
            _log.Debug("Starting all SQS MQ Server worker threads...");

            foreach (var worker in _workers)
            {
                try
                {
                    worker.Start();
                }
                catch (Exception ex)
                {
                    if (this.ErrorHandler != null)
                    {
                        this.ErrorHandler(ex);
                    }

                    _log.Warn(String.Concat("Could not START SQS MQ worker thread: ", ex.Message));
                }
            }
        }

        private void StopWorkerThreads()
        {
            _log.Debug(String.Concat("Stopping all ", _typeName, " MQ Server worker threads..."));

            foreach (var worker in _workers)
            {
                try
                {
                    worker.Stop();
                }
                catch (Exception ex)
                {
                    if (this.ErrorHandler != null)
                    {
                        this.ErrorHandler(ex);
                    }
                    
                    _log.Warn(String.Concat("Could not STOP ", _typeName, " MQ worker thread: ", ex.Message));
                }
            }
        }

        private void KillBgThreadIfExists()
        {
            if (_bgThread == null || !_bgThread.IsAlive)
            {
                return;
            }

            try
            {
                if (!_bgThread.Join(500))
                {
                    _log.Warn(String.Concat("Interrupting previous Background Thread: ", _bgThread.Name));

                    _bgThread.Interrupt();

                    if (!_bgThread.Join(TimeSpan.FromSeconds(3)))
                    {
                        _log.Warn(String.Concat(_bgThread.Name, " just wont die, so we're now aborting it..."));
                        _bgThread.Abort();
                    }
                }

            }
            finally
            {
                _bgThread = null;
            }
        }

        private void RunLoop()
        {
            if (Interlocked.CompareExchange(ref _status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting)
            {
                return;
            }

            Interlocked.Increment(ref _timesStarted);

            try
            {
                lock (_msgLock)
                {
                    // Reset
                    while (Interlocked.CompareExchange(ref _status, 0, 0) == WorkerStatus.Started)
                    {
                        Monitor.Wait(_msgLock);
                        _log.Debug("msgLock received...");

                        var op = Interlocked.CompareExchange(ref _doOperation, WorkerOperation.NoOp, _doOperation);

                        switch (op)
                        {
                            case WorkerOperation.Stop:
                                _log.Debug("Stop Command Issued");

                                if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                {
                                    Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Stopping);
                                }

                                StopWorkerThreads();
                                return;

                            case WorkerOperation.Restart:
                                _log.Debug("Restart Command Issued");

                                if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                {
                                    Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Stopping);
                                }

                                StopWorkerThreads();
                                StartWorkerThreads();

                                Interlocked.CompareExchange(ref _status, WorkerStatus.Started, WorkerStatus.Stopped);
                                
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lastExMsg = ex.Message;
                Interlocked.Increment(ref _noOfErrors);
                Interlocked.Increment(ref _noOfContinuousErrors);

                if (Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                {
                    Interlocked.CompareExchange(ref _status, WorkerStatus.Stopped, WorkerStatus.Stopping);
                }

                StopWorkerThreads();

                if (this.ErrorHandler != null)
                {
                    this.ErrorHandler(ex);
                }

                if (KeepAliveRetryAfterMs.HasValue)
                {
                    Thread.Sleep(KeepAliveRetryAfterMs.Value);
                    Start();
                }
            }

            _log.Debug("Exiting RunLoop()...");
        }
    }
}
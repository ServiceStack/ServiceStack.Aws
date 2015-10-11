using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public partial class PocoDynamo
    {
        //Error Handling: http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ErrorHandling.html
        public void Exec(Action fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
        {
            Exec(() => {
                fn();
                return true;
            }, rethrowExceptions, retryOnErrorCodes);
        }

        public T Exec<T>(Func<T> fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
        {
            var i = 0;
            Exception originalEx = null;
            var firstAttempt = DateTime.UtcNow;

            bool retry = false;

            if (retryOnErrorCodes == null)
                retryOnErrorCodes = RetryOnErrorCodes;

            while (DateTime.UtcNow - firstAttempt < MaxRetryOnExceptionTimeout)
            {
                i++;
                try
                {
                    return fn();
                }
                catch (Exception outerEx)
                {
                    var ex = outerEx.UnwrapIfSingleException();

                    if (rethrowExceptions != null)
                    {
                        foreach (var rethrowEx in rethrowExceptions)
                        {
                            if (ex.GetType().IsAssignableFromType(rethrowEx))
                            {
                                if (ex != outerEx)
                                    throw ex;

                                throw;
                            }
                        }
                    }

                    if (originalEx == null)
                        originalEx = ex;

                    var amazonEx = ex as AmazonDynamoDBException;
                    if (amazonEx != null)
                    {
                        if (amazonEx.StatusCode == HttpStatusCode.BadRequest &&
                            !retryOnErrorCodes.Contains(amazonEx.ErrorCode))
                            throw;
                    }

                    i.SleepBackOffMultiplier();
                }
            }

            throw new TimeoutException("Exceeded timeout of {0}".Fmt(MaxRetryOnExceptionTimeout), originalEx);
        }

        public bool WaitForTablesToBeReady(IEnumerable<string> tableNames, TimeSpan? timeout = null)
        {
            var pendingTables = new List<string>(tableNames);

            if (pendingTables.Count == 0)
                return true;

            var startAt = DateTime.UtcNow;
            do
            {
                try
                {
                    var responses = pendingTables.Map(x =>
                        Exec(() => DynamoDb.DescribeTable(x)));

                    foreach (var response in responses)
                    {
                        if (response.Table.TableStatus == DynamoStatus.Active)
                            pendingTables.Remove(response.Table.TableName);
                    }

                    if (Log.IsDebugEnabled)
                        Log.Debug("Tables Pending: {0}".Fmt(pendingTables.ToJsv()));

                    if (pendingTables.Count == 0)
                        return true;

                    if (timeout != null && DateTime.UtcNow - startAt > timeout.Value)
                        return false;

                    Thread.Sleep(PollTableStatus);
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (true);
        }

        public bool WaitForTablesToBeDeleted(IEnumerable<string> tableNames, TimeSpan? timeout = null)
        {
            var pendingTables = new List<string>(tableNames);

            if (pendingTables.Count == 0)
                return true;

            var startAt = DateTime.UtcNow;
            do
            {
                var existingTables = GetTableNames();
                pendingTables.RemoveAll(x => !existingTables.Contains(x));

                if (Log.IsDebugEnabled)
                    Log.DebugFormat("Waiting for Tables to be removed: {0}", pendingTables.Dump());

                if (pendingTables.Count == 0)
                    return true;

                if (timeout != null && DateTime.UtcNow - startAt > timeout.Value)
                    return false;

                Thread.Sleep(PollTableStatus);

            } while (true);
        }
    }
}
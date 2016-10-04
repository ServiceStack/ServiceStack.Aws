// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb
{
    public partial class PocoDynamo
    {
        private Task CreateTableAsync(DynamoMetadataType table)
        {
            var request = ToCreateTableRequest(table);
            try
            {
                return ExecAsync(() => DynamoDb.CreateTableAsync(request));
            }
            catch (AmazonDynamoDBException ex)
            {
                if (ex.ErrorCode == DynamoErrors.AlreadyExists)
                    return PclExportClient.EmptyTask;

                throw;
            }
        }

        public async Task CreateMissingTablesAsync(IEnumerable<DynamoMetadataType> tables, CancellationToken token = default(CancellationToken))
        {
            var tablesList = tables.Safe().ToList();
            if (tablesList.Count == 0)
                return;

            var existingTableNames = GetTableNames().ToList();

            foreach (var table in tablesList)
            {
                if (existingTableNames.Contains(table.Name))
                    continue;

                if (Log.IsDebugEnabled)
                    Log.Debug("Creating Table: " + table.Name);

                await CreateTableAsync(table);
            }

            await WaitForTablesToBeReadyAsync(tablesList.Map(x => x.Name), token);
        }

        public async Task WaitForTablesToBeReadyAsync(IEnumerable<string> tableNames, CancellationToken token = default(CancellationToken))
        {
            var pendingTables = new List<string>(tableNames);

            if (pendingTables.Count == 0)
                return;

            do
            {
                try
                {
                    var responses = await Task.WhenAll(pendingTables.Map(x =>
                            ExecAsync(() => DynamoDb.DescribeTableAsync(x, token))
                        ).ToArray());

                    foreach (var response in responses)
                    {
                        if (response.Table.TableStatus == DynamoStatus.Active)
                            pendingTables.Remove(response.Table.TableName);
                    }

                    if (Log.IsDebugEnabled)
                        Log.Debug($"Tables Pending: {pendingTables.ToJsv()}");

                    if (pendingTables.Count == 0)
                        return;

                    if (token.IsCancellationRequested)
                        return;

                    Thread.Sleep(PollTableStatus);
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (true);
        }

        public Task InitSchemaAsync()
        {
            return CreateMissingTablesAsync(DynamoMetadata.GetTables());
        }
    }
}
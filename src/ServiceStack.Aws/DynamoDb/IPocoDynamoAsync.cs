// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Aws.DynamoDb
{
    /// <summary>
    /// Available API's with Async equivalents
    /// </summary>
    public interface IPocoDynamoAsync
    {
        Task CreateMissingTablesAsync(IEnumerable<DynamoMetadataType> tables, 
            CancellationToken token = default(CancellationToken));

        Task WaitForTablesToBeReadyAsync(IEnumerable<string> tableNames, 
            CancellationToken token = default(CancellationToken));

        Task InitSchemaAsync();
    }
}
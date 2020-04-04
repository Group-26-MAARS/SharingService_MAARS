// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharingService.Data
{
    public class RouteCacheEntity : TableEntity
    {
        public RouteCacheEntity() { }
        public RouteCacheEntity(long routeId, int partitionSize)
        {
            this.PartitionKey = (routeId / partitionSize).ToString();
            this.RowKey = routeId.ToString();
        }
        public string RouteKey { get; set; }

    }
    public class AnchorCacheEntity : TableEntity
    {
        public AnchorCacheEntity() { }

        public AnchorCacheEntity(long anchorId, int partitionSize)
        {
            this.PartitionKey = (anchorId / partitionSize).ToString();
            this.RowKey = anchorId.ToString();
        }

        public string AnchorKey { get; set; }
    }


    internal class CosmosDbCache : IAnchorKeyCache
    {
        /// <summary>
        /// Super basic partitioning scheme
        /// </summary>
        private const int partitionSize = 500;

        /// <summary>
        /// The database cache for anchors.
        /// </summary>
        private readonly CloudTable dbCache;

        /// <summary>
        /// The anchor numbering index.
        /// </summary>
        private long lastAnchorNumberIndex = -1;

        // To ensure our asynchronous initialization code is only ever invoked once, we employ two manualResetEvents
        ManualResetEventSlim initialized = new ManualResetEventSlim();
        ManualResetEventSlim initializing = new ManualResetEventSlim();

        private async Task InitializeAsync()
        {
            if (!this.initialized.Wait(0))
            {
                if (!this.initializing.Wait(0))
                {
                    this.initializing.Set();
                    await this.dbCache.CreateIfNotExistsAsync();

                    this.initialized.Set();
                }
                this.initialized.Wait();
            }
        }

        public CosmosDbCache(string storageConnectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            this.dbCache = tableClient.GetTableReference("AnchorCache");
        }

        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public async Task<bool> ContainsAsync(long anchorId)
        {
            await this.InitializeAsync();

            TableResult result = await this.dbCache.ExecuteAsync(TableOperation.Retrieve<AnchorCacheEntity>((anchorId / CosmosDbCache.partitionSize).ToString(), anchorId.ToString()));
            AnchorCacheEntity anchorEntity = result.Result as AnchorCacheEntity;
            return anchorEntity != null;
        }

        // For CosmosDbCache.cs
        /// <summary>
        /// Gets all anchors in DB
        /// </summary>
        /// <returns>The anchor.</returns>
        public async Task<List<AnchorCacheEntity>> GetAllAnchorsAsync()
        {
            await this.InitializeAsync();

            List<AnchorCacheEntity> results = new List<AnchorCacheEntity>();
            TableQuery<AnchorCacheEntity> tableQuery = new TableQuery<AnchorCacheEntity>();
            TableQuerySegment<AnchorCacheEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<AnchorCacheEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<AnchorCacheEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.ToList();
        }
        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The anchor key.</returns>
        public async Task<string> GetAnchorKeyAsync(long anchorId)
        {
            await this.InitializeAsync();

            TableResult result = await this.dbCache.ExecuteAsync(TableOperation.Retrieve<AnchorCacheEntity>((anchorId / CosmosDbCache.partitionSize).ToString(), anchorId.ToString()));
            AnchorCacheEntity anchorEntity = result.Result as AnchorCacheEntity;
            if (anchorEntity != null)
            {
                return anchorEntity.AnchorKey;
            }

            throw new KeyNotFoundException($"The {nameof(anchorId)} {anchorId} could not be found.");
        }

        /// <summary>
        /// Gets the last anchor asynchronously.
        /// </summary>
        /// <returns>The anchor.</returns>
        public async Task<AnchorCacheEntity> GetLastAnchorAsync()
        {
            await this.InitializeAsync();

            List<AnchorCacheEntity> results = new List<AnchorCacheEntity>();
            TableQuery<AnchorCacheEntity> tableQuery = new TableQuery<AnchorCacheEntity>();
            TableQuerySegment<AnchorCacheEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<AnchorCacheEntity> currentSegment = await this.dbCache.ExecuteQuerySegmentedAsync<AnchorCacheEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.OrderByDescending(x => x.Timestamp).DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Gets all keys asynchronously.
        /// </summary>
        /// <returns>The anchor key.</returns>
        /// 
        public async Task<List<AnchorCacheEntity>> GetAllAnchorKeysAsync()
        {
            List<string> myList = new List<string>();
            List<AnchorCacheEntity> anchorCacheList = await this.GetAllAnchorsAsync();
            /*
            foreach (AnchorCacheEntity anchorCacheEntity in anchorCacheList)
            {
                myList.Add(anchorCacheEntity?.AnchorKey);
            }
            */
            return anchorCacheList;
        }

        /// <summary>
        /// Gets the last anchor key asynchronously.
        /// </summary>
        /// <returns>The anchor key.</returns>
        public async Task<string> GetLastAnchorKeyAsync()
        {
            return (await this.GetLastAnchorAsync())?.AnchorKey;
        }

        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorKey">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the anchor identifier.</returns>
        public async Task<long> SetAnchorKeyAsync(string anchorKey)
        {
            await this.InitializeAsync();

            if (lastAnchorNumberIndex == long.MaxValue)
            {
                // Reset the anchor number index.
                lastAnchorNumberIndex = -1;
            }

            if(lastAnchorNumberIndex < 0)
            {
                // Query last row key
                var rowKey = (await this.GetLastAnchorAsync())?.RowKey;
                long.TryParse(rowKey, out lastAnchorNumberIndex);
            }

            long newAnchorNumberIndex = ++lastAnchorNumberIndex;

            AnchorCacheEntity anchorEntity = new AnchorCacheEntity(newAnchorNumberIndex, CosmosDbCache.partitionSize)
            {
                AnchorKey = anchorKey
            };

            await this.dbCache.ExecuteAsync(TableOperation.Insert(anchorEntity));

            return newAnchorNumberIndex;
        }
    }

    internal class CosmosRouteCache : IRouteKeyCache
    {
        /// <summary>
        /// Super basic partitioning scheme
        /// </summary>
        private const int partitionSize = 500;

        /// <summary>
        /// The database cache for routes.
        /// </summary>
        private readonly CloudTable routesCache;

        /// <summary>
        /// The route numbering index.
        /// </summary>
        private long lastRouteNumberIndex = -1;

        // To ensure our asynchronous initialization code is only ever invoked once, we employ two manualResetEvents
        ManualResetEventSlim initialized = new ManualResetEventSlim();
        ManualResetEventSlim initializing = new ManualResetEventSlim();

        private async Task InitializeAsync()
        {
            if (!this.initialized.Wait(0))
            {
                if (!this.initializing.Wait(0))
                {
                    this.initializing.Set();
                    await this.routesCache.CreateIfNotExistsAsync();

                    this.initialized.Set();
                }
                this.initialized.Wait();
            }
        }

        public CosmosRouteCache(string storageConnectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            this.routesCache = tableClient.GetTableReference("RoutesCache");
        }

        /// <summary>
        /// Determines whether the cache contains the specified route identifier.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public async Task<bool> ContainsAsync(long routeId)
        {
            await this.InitializeAsync();

            TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Retrieve<RouteCacheEntity>((routeId / CosmosRouteCache.partitionSize).ToString(), routeId.ToString()));
            RouteCacheEntity routeEntity = result.Result as RouteCacheEntity;
            return routeEntity != null;
        }

        // For CosmosDbCache.cs
        /// <summary>
        /// Gets all routes in DB
        /// </summary>
        /// <returns>The route.</returns>
        public async Task<List<RouteCacheEntity>> GetAllRoutesAsync()
        {
            await this.InitializeAsync();

            List<RouteCacheEntity> results = new List<RouteCacheEntity>();
            TableQuery<RouteCacheEntity> tableQuery = new TableQuery<RouteCacheEntity>();
            TableQuerySegment<RouteCacheEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<RouteCacheEntity> currentSegment = await this.routesCache.ExecuteQuerySegmentedAsync<RouteCacheEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.ToList();
        }
        /// <summary>
        /// Gets the route key asynchronously.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The route key.</returns>
        public async Task<string> GetRouteKeyAsync(long routeId)
        {
            await this.InitializeAsync();

            TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Retrieve<RouteCacheEntity>((routeId / CosmosRouteCache.partitionSize).ToString(), routeId.ToString()));
            RouteCacheEntity routeEntity = result.Result as RouteCacheEntity;
            if (routeEntity != null)
            {
                return routeEntity.RouteKey;
            }

            throw new KeyNotFoundException($"The {nameof(routeId)} {routeId} could not be found.");
        }

        /// <summary>
        /// Gets the last route asynchronously.
        /// </summary>
        /// <returns>The route.</returns>
        public async Task<RouteCacheEntity> GetLastRouteAsync()
        {
            await this.InitializeAsync();

            List<RouteCacheEntity> results = new List<RouteCacheEntity>();
            TableQuery<RouteCacheEntity> tableQuery = new TableQuery<RouteCacheEntity>();
            TableQuerySegment<RouteCacheEntity> previousSegment = null;
            while (previousSegment == null || previousSegment.ContinuationToken != null)
            {
                TableQuerySegment<RouteCacheEntity> currentSegment = await this.routesCache.ExecuteQuerySegmentedAsync<RouteCacheEntity>(tableQuery, previousSegment?.ContinuationToken);
                previousSegment = currentSegment;
                results.AddRange(previousSegment.Results);
            }

            return results.OrderByDescending(x => x.Timestamp).DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Gets all keys asynchronously.
        /// </summary>
        /// <returns>The route key.</returns>
        /// 
        public async Task<List<RouteCacheEntity>> GetAllRouteKeysAsync()
        {
            List<string> myList = new List<string>();
            List<RouteCacheEntity> routeCacheList = await this.GetAllRoutesAsync();
            /*
            foreach (RoutesCacheEntity routeCacheEntity in routeCacheList)
            {
                myList.Add(routesCacheEntity?.RouteKey);
            }
            */
            return routeCacheList;
        }

        /// <summary>
        /// Gets the last route key asynchronously.
        /// </summary>
        /// <returns>The route key.</returns>
        public async Task<string> GetLastRouteKeyAsync()
        {
            return (await this.GetLastRouteAsync())?.RouteKey;
        }

        /// <summary>
        /// Sets the route key asynchronously.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the route identifier.</returns>
        public async Task<long> SetRouteKeyAsync(string routeKey)
        {
            await this.InitializeAsync();

            if (lastRouteNumberIndex == long.MaxValue)
            {
                // Reset the route number index.
                lastRouteNumberIndex = -1;
            }

            if (lastRouteNumberIndex < 0)
            {
                // Query last row key
                var rowKey = (await this.GetLastRouteAsync())?.RowKey;
                long.TryParse(rowKey, out lastRouteNumberIndex);
            }

            long newRouteNumberIndex = ++lastRouteNumberIndex;

            RouteCacheEntity routeEntity = new RouteCacheEntity(newRouteNumberIndex, CosmosRouteCache.partitionSize)
            {
                RouteKey = routeKey
            };

            await this.routesCache.ExecuteAsync(TableOperation.Insert(routeEntity));

            return newRouteNumberIndex;
        }
    }
}
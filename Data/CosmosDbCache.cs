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
        public RouteCacheEntity(int partitionSize, string routeName)
        {
            //this.PartitionKey = (routeId / partitionSize).ToString();
            this.PartitionKey = "0";
            this.RowKey = routeName;
        }
        public string AnchorIdentifiers { get; set; }

    }
    public class AnchorCacheEntity : TableEntity
    {
        public AnchorCacheEntity() { }

        public AnchorCacheEntity(int partitionSize, string anchorName, string location, string expiration, string description)
        {
            //this.PartitionKey = (anchorId / partitionSize).ToString();
            this.PartitionKey = "0";
            //this.AnchorName = anchorName; // commenting out -redundant
            this.Location = location;
            this.Expiration = expiration;
            this.Description = description;

            this.RowKey = anchorName;
        }
        public string Location { get; set; }
        public string Expiration { get; set; }
        public string Description { get; set; }
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
        public async Task<string> GetAnchorKeyAsync(string anchorName)
        {
            await this.InitializeAsync();

            TableResult result = await this.dbCache.ExecuteAsync(TableOperation.Retrieve<AnchorCacheEntity>("0", anchorName));
            AnchorCacheEntity anchorEntity = result.Result as AnchorCacheEntity;
            if (anchorEntity != null)
            {
                return anchorEntity.RowKey + ":" + anchorEntity.AnchorKey + ":" + anchorEntity.Location + ":" + anchorEntity.Expiration + ":" + anchorEntity.Description;
            }

            throw new KeyNotFoundException($"The {nameof(anchorName)} {anchorName} could not be found.");
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
        public async Task<string> SetAnchorKeyAsync(string anchorKey, string anchorName, string location, string expiration, string description)
        {
            await this.InitializeAsync();

            if (lastAnchorNumberIndex == long.MaxValue)
            {
                // Reset the anchor number index.
                lastAnchorNumberIndex = -1;
            }

            /*
            if(lastAnchorNumberIndex < 0)
            {
                // Query last row key
                var rowKey = (await this.GetLastAnchorAsync())?.RowKey;
                long.TryParse(rowKey, out lastAnchorNumberIndex);
            }
            */

            long newAnchorNumberIndex = ++lastAnchorNumberIndex;

            // Elimiating "RowKey" from Cosmos. Now going to just use the row name (insert or update)

            AnchorCacheEntity anchorEntity = new AnchorCacheEntity(CosmosDbCache.partitionSize, anchorName, location, expiration, description)
            {
                AnchorKey = anchorKey,
                //AnchorName = anchorName,
                Location = location,
                Expiration = expiration,
                Description = description
            };

            await this.dbCache.ExecuteAsync(TableOperation.InsertOrReplace(anchorEntity));

            //return newAnchorNumberIndex;
            return anchorName;
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
        public async Task<string> GetRouteKeyAsync(string routeName)
        {
            await this.InitializeAsync();

            //TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Retrieve<RouteCacheEntity>((routeId / CosmosRouteCache.partitionSize).ToString(), routeId.ToString()));
            TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Retrieve<RouteCacheEntity>("0", routeName));

            RouteCacheEntity routeEntity = result.Result as RouteCacheEntity;
            if (routeEntity != null)
            {
                return routeEntity.AnchorIdentifiers;
            }

            throw new KeyNotFoundException($"The {nameof(routeName)} {routeName} could not be found.");
        }

        /// <summary>
        /// Deletes the route key asynchronously.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The route key.</returns>
        public async Task<string> DeleteRouteKeyAsync(string routeName)
        {
            await this.InitializeAsync();
            RouteCacheEntity routeCacheEntity = new RouteCacheEntity();
            routeCacheEntity.RowKey = routeName;
            routeCacheEntity.PartitionKey = "0";
            routeCacheEntity.ETag = "*";
            //TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Retrieve<RouteCacheEntity>((routeId / CosmosRouteCache.partitionSize).ToString(), routeId.ToString()));
            TableResult result = await this.routesCache.ExecuteAsync(TableOperation.Delete(routeCacheEntity));

            RouteCacheEntity routeEntity = result.Result as RouteCacheEntity;
            if (routeEntity != null)
            {
                return routeName;
            }

            throw new KeyNotFoundException($"The {nameof(routeName)} {routeName} could not be found.");
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
            return (await this.GetLastRouteAsync())?.AnchorIdentifiers;
        }

        /// <summary>
        /// Sets the route key asynchronously.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the route identifier.</returns>
        public async Task<string> SetRouteKeyAsync(string routeName, string AnchorIdentifiers)
        {
            await this.InitializeAsync();
            /*
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
            */

            //long newRouteNumberIndex = ++lastRouteNumberIndex;

            RouteCacheEntity routeEntity = new RouteCacheEntity(CosmosRouteCache.partitionSize, routeName)
            {
                AnchorIdentifiers = AnchorIdentifiers,
            };

            await this.routesCache.ExecuteAsync(TableOperation.InsertOrReplace(routeEntity));

            return routeName;
        }
    }
}
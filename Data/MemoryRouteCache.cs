// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharingService.Data
{
    internal class MemoryRouteCache : IRouteKeyCache
    {
        /// <summary>
        /// The entry cache options.
        /// </summary>
        private static readonly MemoryCacheEntryOptions entryCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(48),
        };

        /// <summary>
        /// The memory cache.
        /// </summary>
        private readonly MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// The route numbering index.
        /// </summary>
        private long routeNumberIndex = -1;

        /// <summary>
        /// Determines whether the cache contains the specified route identifier.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public Task<bool> ContainsAsync(long routeId)
        {
            return Task.FromResult(this.memoryCache.TryGetValue(routeId, out _));
        }

        /// <summary>
        /// Gets the route key asynchronously.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The route key.</returns>
        public Task<string> GetRouteKeyAsync(long routeId)
        {
            if (this.memoryCache.TryGetValue(routeId, out string routeKey))
            {
                return Task.FromResult(routeKey);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(routeId)} {routeId} could not be found."));
        }


        /// <summary>
        /// Gets the last route key asynchronously.
        /// </summary>
        /// <returns>The route key.</returns>
        public Task<List<RouteCacheEntity>> GetAllRouteKeysAsync()
        {
            return Task.FromResult<List<RouteCacheEntity>>(null);
        }

        /// <summary>
        /// Gets the last route key asynchronously.
        /// </summary>
        /// <returns>The route key.</returns>
        public Task<string> GetLastRouteKeyAsync()
        {
            if (this.routeNumberIndex >= 0 && this.memoryCache.TryGetValue(this.routeNumberIndex, out string routeKey))
            {
                return Task.FromResult(routeKey);
            }

            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Sets the route key asynchronously.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the route identifier.</returns>
        public Task<long> SetRouteKeyAsync(string routeKey)
        {
            if (this.routeNumberIndex == long.MaxValue)
            {
                // Reset the route number index.
                this.routeNumberIndex = -1;
            }

            long newRouteNumberIndex = ++this.routeNumberIndex;
            this.memoryCache.Set(newRouteNumberIndex, routeKey, entryCacheOptions);

            return Task.FromResult(newRouteNumberIndex);
        }
    }
}

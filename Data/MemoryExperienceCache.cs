// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharingService.Data
{
    internal class MemoryExperienceCache : IExperienceKeyCache
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
        /// The experience numbering index.
        /// </summary>
        private long experienceNumberIndex = -1;

        /// <summary>
        /// Determines whether the cache contains the specified experience identifier.
        /// </summary>
        /// <param name="experienceId">The experience identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public Task<bool> ContainsAsync(long experienceId)
        {
            return Task.FromResult(this.memoryCache.TryGetValue(experienceId, out _));
        }


        /// <summary>
        /// Delete the experience key asynchronously.
        /// </summary>
        /// <param name="experienceId">The experience identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The experience key.</returns>
        public Task<string> DeleteExperienceKeyAsync(string experienceName)
        {
            if (this.memoryCache.TryGetValue(experienceName, out string anchorIdentifiers))
            {
                return Task.FromResult(anchorIdentifiers);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(experienceName)} {experienceName} could not be found."));
        }


        /// <summary>
        /// Gets the experience key asynchronously.
        /// </summary>
        /// <param name="experienceId">The experience identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The experience key.</returns>
        public Task<string> GetExperienceKeyAsync(string experienceName)
        {
            if (this.memoryCache.TryGetValue(experienceName, out string anchorIdentifiers))
            {
                return Task.FromResult(anchorIdentifiers);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(experienceName)} {experienceName} could not be found."));
        }


        /// <summary>
        /// Gets the last experience key asynchronously.
        /// </summary>
        /// <returns>The experience key.</returns>
        public Task<List<ExperienceCacheEntity>> GetAllExperienceKeysAsync()
        {
            return Task.FromResult<List<ExperienceCacheEntity>>(null);
        }

        /// <summary>
        /// Gets the last experience key asynchronously.
        /// </summary>
        /// <returns>The experience key.</returns>
        public Task<string> GetLastExperienceKeyAsync()
        {
            if (this.experienceNumberIndex >= 0 && this.memoryCache.TryGetValue(this.experienceNumberIndex, out string anchorIdentifiers))
            {
                return Task.FromResult(anchorIdentifiers);
            }

            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Sets the experience key asynchronously.
        /// </summary>
        /// <param name="experienceKey">The experience key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the experience identifier.</returns>
        public Task<string> SetExperienceKeyAsync(string experienceName, string anchorIdentifiers)
        {
            if (this.experienceNumberIndex == long.MaxValue)
            {
                // Reset the experience number index.
                this.experienceNumberIndex = -1;
            }

            long newExperienceNumberIndex = ++this.experienceNumberIndex;
            this.memoryCache.Set(newExperienceNumberIndex, anchorIdentifiers, entryCacheOptions);

            //return Task.FromResult(newExperienceNumberIndex);
            return Task.FromResult(experienceName);
        }
    }
}

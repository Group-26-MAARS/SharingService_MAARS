// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharingService.Data
{
    internal class MemoryAnimationCache : IAnimationKeyCache
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
        /// The animation numbering index.
        /// </summary>
        private long animationNumberIndex = -1;

        /// <summary>
        /// Determines whether the cache contains the specified animation identifier.
        /// </summary>
        /// <param name="animationId">The animation identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public Task<bool> ContainsAsync(long animationId)
        {
            return Task.FromResult(this.memoryCache.TryGetValue(animationId, out _));
        }

        /// <summary>
        /// Gets the animation key asynchronously.
        /// </summary>
        /// <param name="animationID">The animation identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The animation key.</returns>
        public Task<string> GetAnimationKeyAsync(string animationName)
        {
            if (this.memoryCache.TryGetValue(animationName, out string animationKey))
            {
                return Task.FromResult(animationKey);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(animationName)} {animationName} could not be found."));
        }

        /// <summary>
        /// Delete the animation key asynchronously.
        /// </summary>
        /// <param name="animationId">The animation identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The route key.</returns>
        public Task<string> DeleteAnimationKeyAsync(string animationName)
        {
            if (this.memoryCache.TryGetValue(animationName, out string animationIdentifiers))
            {
                return Task.FromResult(animationIdentifiers);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(animationName)} {animationName} could not be found."));
        }


        /// <summary>
        /// Gets the last animation key asynchronously.
        /// </summary>
        /// <returns>The animation key.</returns>
        public Task<List<AnimationCacheEntity>> GetAllAnimationKeysAsync()
        {
            return Task.FromResult<List<AnimationCacheEntity>>(null);
        }
        /*
        /// <summary>
        /// Gets the last animation key asynchronously.
        /// </summary>
        /// <returns>The animation key.</returns>
        public Task<string> GetLastAnimationKeyAsync()
        {
            if (this.anchorNumberIndex >= 0 && this.memoryCache.TryGetValue(this.animationNumberIndex, out string anchorKey))
            {
                return Task.FromResult(anchorKey);
            }

            return Task.FromResult<string>(null);
        }
        */

        /// <summary>
        /// Sets the animation key asynchronously.
        /// </summary>
        /// <param name="animationKey">The animation key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the animation identifier.</returns>
        public Task<string> SetAnimationKeyAsync(string animationKey, string animationName)
        {
            if (this.animationNumberIndex == long.MaxValue)
            {
                // Reset the animation number index.
                this.animationNumberIndex = -1;
            }

            long newAnimationNumberIndex = ++this.animationNumberIndex;
            this.memoryCache.Set(newAnimationNumberIndex, animationKey, entryCacheOptions);

            //return Task.FromResult(newAnchorNumberIndex);
            return Task.FromResult(animationName);
        }
    }
}

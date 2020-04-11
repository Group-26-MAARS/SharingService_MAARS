// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SharingService.Data
{
    /// <summary>
    /// An interface representing an route key cache.
    /// </summary>
    public interface IAnimationKeyCache
    {
        /// <summary>
        /// Determines whether the cache contains the specified animation identifier.
        /// </summary>
        /// <param name="routeId">The animation identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}"/> containing true if the identifier is found; otherwise false.</returns>
        Task<bool> ContainsAsync(long animationId);

        /// <summary>
        /// Gets the animation key asynchronously.
        /// </summary>
        /// <param name="animationName">The animation identifier.</param>
        /// <returns>The animation key.</returns>
        Task<string> GetAnimationKeyAsync(string animationName);

        /// <summary>
        /// Deletes the animation key asynchronously.
        /// </summary>
        /// <param name="routeName">The animation identifier.</param>
        /// <returns>The animation key.</returns>
        Task<string> DeleteAnimationKeyAsync(string animationName);

        /// <summary>
        /// Gets all animations in DB
        /// </summary>
        /// <returns>The animation.</returns>
        Task<List<AnimationCacheEntity>> GetAllAnimationKeysAsync();


        /// <summary>
        /// Sets the animation key asynchronously.
        /// </summary>
        /// <param name="animationKey">The animation key.</param>
        /// <returns>An <see cref="Task{System.Int64}"/> representing the animation identifier.</returns>
        Task<string> SetAnimationKeyAsync(string animationName, string animationIdentifiers);
    }
}

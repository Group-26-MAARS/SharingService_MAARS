// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SharingService.Data
{
    /// <summary>
    /// An interface representing an route key cache.
    /// </summary>
    public interface IAnchorKeyCache
    {
        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}"/> containing true if the identifier is found; otherwise false.</returns>
        Task<bool> ContainsAsync(long anchorId);

        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>The anchor key.</returns>
        Task<string> GetAnchorKeyAsync(string anchorName);

        /// <summary>
        /// Gets all anchors in DB
        /// </summary>
        /// <returns>The anchor.</returns>
        Task<List<AnchorCacheEntity>> GetAllAnchorKeysAsync();

        /// <summary>
        /// Gets the last anchor key asynchronously
        /// </summary>
        /// <returns>The last anchor key stored if available; otherwise, null.</returns>
        Task<string> GetLastAnchorKeyAsync();

        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorKey">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}"/> representing the anchor identifier.</returns>
        Task<string> SetAnchorKeyAsync(string anchorKey, string anchorName, string location, string expiration, string description);
    }
}

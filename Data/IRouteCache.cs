// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SharingService.Data
{
    /// <summary>
    /// An interface representing an route key cache.
    /// </summary>
    public interface IRouteKeyCache
    {
        /// <summary>
        /// Determines whether the cache contains the specified route identifier.
        /// </summary>
        /// <param name="routeId">The route identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}"/> containing true if the identifier is found; otherwise false.</returns>
        Task<bool> ContainsAsync(long routeId);

        /// <summary>
        /// Gets the route key asynchronously.
        /// </summary>
        /// <param name="routeName">The route identifier.</param>
        /// <returns>The route key.</returns>
        Task<string> GetRouteKeyAsync(string routeName);

        /// <summary>
        /// Deletes the route key asynchronously.
        /// </summary>
        /// <param name="routeName">The route identifier.</param>
        /// <returns>The route key.</returns>
        Task<string> DeleteRouteKeyAsync(string routeName);

        /// <summary>
        /// Gets all routes in DB
        /// </summary>
        /// <returns>The route.</returns>
        Task<List<RouteCacheEntity>> GetAllRouteKeysAsync();

        /// <summary>
        /// Gets the last route key asynchronously.
        /// </summary>
        /// <returns>The last route key stored if available; otherwise, null.</returns>
        Task<string> GetLastRouteKeyAsync();

        /// <summary>
        /// Sets the route key asynchronously.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <returns>An <see cref="Task{System.Int64}"/> representing the route identifier.</returns>
        Task<string> SetRouteKeyAsync(string routeName, string anchorIdentifiers);
    }
}

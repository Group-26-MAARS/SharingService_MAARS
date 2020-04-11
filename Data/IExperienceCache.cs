// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SharingService.Data
{
    /// <summary>
    /// An interface representing an experience key cache.
    /// </summary>
    public interface IExperienceKeyCache
    {
        /// <summary>
        /// Determines whether the cache contains the specified experience identifier.
        /// </summary>
        /// <param name="experienceId">The experience identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}"/> containing true if the identifier is found; otherwise false.</returns>
        Task<bool> ContainsAsync(long experienceId);

        /// <summary>
        /// Gets the experience key asynchronously.
        /// </summary>
        /// <param name="experienceName">The experience identifier.</param>
        /// <returns>The experience key.</returns>
        Task<string> GetExperienceKeyAsync(string experienceName);

        /// <summary>
        /// Deletes the experience key asynchronously.
        /// </summary>
        /// <param name="experienceName">The experience identifier.</param>
        /// <returns>The experience key.</returns>
        Task<string> DeleteExperienceKeyAsync(string experienceName);

        /// <summary>
        /// Gets all experiences in DB
        /// </summary>
        /// <returns>The experience.</returns>
        Task<List<ExperienceCacheEntity>> GetAllExperienceKeysAsync();

        /// <summary>
        /// Gets the last experience key asynchronously.
        /// </summary>
        /// <returns>The last experience key stored if available; otherwise, null.</returns>
        Task<string> GetLastExperienceKeyAsync();

        /// <summary>
        /// Sets the experience key asynchronously.
        /// </summary>
        /// <param name="experienceKey">The experience key.</param>
        /// <returns>An <see cref="Task{System.Int64}"/> representing the experience identifier.</returns>
        Task<string> SetExperienceKeyAsync(string experienceName, string anchorIdentifiers);
    }
}

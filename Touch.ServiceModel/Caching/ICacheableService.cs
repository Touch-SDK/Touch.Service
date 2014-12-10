using System.Collections.Generic;

namespace Touch.Service
{
    /// <summary>
    /// Service that supports caching.
    /// </summary>
    public interface ICacheableService
    {
        /// <summary>
        /// Get cache key for the given operation name and arguments.
        /// </summary>
        /// <param name="operation">Service operation name.</param>
        /// <param name="arguments">Operation arguments.</param>
        /// <returns>Key that is unique for the given operation name and arguments, or <c>null</c> if caching should not be applied.</returns>
        string GetCacheKey(string operation, IList<string> arguments);

        /// <summary>
        /// Get cache metadata for the provided key.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <returns>Cache metadata or <c>null</c> if no metadata is found.</returns>
        ICacheMetadata GetMetadata(string cacheKey);
    }
}

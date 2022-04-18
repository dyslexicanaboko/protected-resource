using ProtectedResource.Lib.Models;
using System;

namespace ProtectedResource.Lib.Services
{
    public interface ICachingService
        : IDisposable
    {
        void Initialize();
        
        /// <summary>
        /// Get a value stored in cache.
        /// </summary>
        /// <param name="key">Key or name where a collection of hashFields are stored.</param>
        /// <param name="hashField">Hash field or name used to retrieve a value.</param>
        /// <returns></returns>
        CachedValue HashGet(string key, string hashField);

        /// <summary>
        /// Store a value to cache.
        /// </summary>
        /// <param name="key">Key or name where a collection of hashFields are stored.</param>
        /// <param name="hashField">Hash field or name used to set a value and retrieve it again later. Think key/value pair.</param>
        /// <param name="value">Value stored in the (key, hashField) location.</param>
        void HashSet(string key, string hashField, string value);

        /// <summary>
        /// Delete a value from cache.
        /// </summary>
        /// <param name="key">Key or name where a collection of hashFields are stored.</param>
        /// <param name="hashField">Hash field or name used to locate what is to be deleted.</param>
        void HashDelete(string key, string hashField);

        /// <summary>
        /// Clear everything from cache. This is intended for integration testing only.
        /// </summary>
        void Clear();
    }
}
using ProtectedResource.Lib.Models;
using ProtectedResource.Lib.Services;
using System.Collections.Generic;

namespace ProtectedResource.UnitTests.Dummy
{
    public class DummyCachingService
        : ICachingService
    {
        public Dictionary<string, string> Cache { get; set; }

        public void Initialize()
        {
            Cache = new Dictionary<string, string>();
        }

        public CachedValue HashGet(string key, string hashField)
        {
            var cv = new CachedValue();

            //For the sake of testing, ignoring the incoming key and only using hashField
            if (Cache.TryGetValue(hashField, out var value))
            {
                cv.Value = value;

                return cv;
            }

            cv.IsNull = true;

            return cv;
        }

        public void HashSet(string key, string hashField, string value)
        {
            //Can't perform expiration here for now. I don't think I need it either.

            //For the sake of testing, ignoring the incoming key and only using hashField
            if (Cache.ContainsKey(hashField))
            {
                Cache[hashField] = value;

                return;
            }

            Cache.Add(hashField, value);
        }

        public void Clear()
        {
            Cache.Clear();
        }

        public void Dispose()
        {
            //Do nothing
        }
    }
}

using ProtectedResource.Lib.Models;

namespace ProtectedResource.Lib.Services
{
    public interface ICachingService
    {
        void Initialize();
        
        CachedValue HashGet(string key, string hashField);

        void HashSet(string key, string hashField, string value);
        
        void Clear();
    }
}
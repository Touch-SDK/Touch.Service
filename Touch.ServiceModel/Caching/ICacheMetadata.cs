using System;

namespace Touch.Service
{
    public interface ICacheMetadata
    {
        string Token { get; }

        DateTime LastModified { get; }
    }

    public sealed class CacheMetadata : ICacheMetadata
    {
        public string Token { get; set; }

        public DateTime LastModified { get; set; }
    }
}

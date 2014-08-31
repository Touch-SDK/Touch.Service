using System;

namespace Touch.ServiceModel.Providers
{
    public sealed class ResponseMetadataProvider
    {
        public static Func<string,IResponseMetadataProvider> Resolver { private get; set; }

        public static IResponseMetadataProvider GetInstance(string name) { return Resolver(name); }
    }

    public interface IResponseMetadataProvider
    {
        string CacheKey { get; }

        string ETag { get; }

        DateTime? LastModified { get; }
    }
}

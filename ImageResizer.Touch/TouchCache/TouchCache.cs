using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using Touch.Providers;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchCache
{
    public sealed class TouchCache : IPlugin, ICache, IMultiInstancePlugin
    {
        #region Dependencies
        public static IDependenciesProvider DependenciesProvider { private get; set; }

        public IStorage Storage { private get; set; }
        #endregion

        #region Data
        private static readonly TimeSpan ExpiresIn = TimeSpan.FromDays(365);
        private readonly ConcurrentDictionary<string, Tuple<string, DateTime>> _cache = new ConcurrentDictionary<string, Tuple<string, DateTime>>();
        #endregion

        #region Public methods
        public IPlugin Install(Config c)
        {
            if (DependenciesProvider == null)
                throw new ConfigurationErrorsException("Missing DependenciesProvider");

            var plugin = DependenciesProvider.Resolve<TouchCache>();

            c.Plugins.add_plugin(plugin);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public bool CanProcess(HttpContext current, IResponseArgs e)
        {
            return e.RewrittenQuerystring.AllKeys.Contains("output");
        }

        public void Process(HttpContext current, IResponseArgs e)
        {
            var key = e.RewrittenQuerystring["output"];
            Tuple<string, DateTime> cached;

            if (_cache.TryGetValue(key, out cached))
            {
                e.ResponseHeaders.Headers["ETag"] = cached.Item1;
                e.ResponseHeaders.LastModified = cached.Item2;
            }
            else
            {
                if (!Storage.HasFile(key))
                {
                    var meta = new Metadata { ContentType = e.RewrittenQuerystring["mime"] };

                    using (var data = new MemoryStream())
                    {
                        e.ResizeImageToStream(data);
                        Storage.PutFile(data, key, meta);
                    }
                }

                var metadata = Storage.GetMetadata(key);

                e.ResponseHeaders.Headers["ETag"] = metadata.ETag;
                e.ResponseHeaders.LastModified = metadata.LastModified;

                _cache.TryAdd(key, new Tuple<string, DateTime>(metadata.ETag, metadata.LastModified));
            }

            if (Storage.IsPublic)
            {
                var url = Storage.GetPublicUrl(key);

                Serve(current, e, () => new WebClient().OpenRead(url));
            }
            else
            {
                Serve(current, e, () => Storage.GetFile(key));
            }
        }
        #endregion

        #region Helper methods
        private static void Serve(HttpContext context, IResponseArgs e, Func<Stream> factory)
        {
            context.RemapHandler(new TouchCacheHandler(e, factory));
        } 
        #endregion
    }
}

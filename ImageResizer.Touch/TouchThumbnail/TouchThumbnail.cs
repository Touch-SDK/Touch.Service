using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using Touch.Providers;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchThumbnail : IPlugin, IMultiInstancePlugin
    {
        #region Dependencies
        public static IDependenciesProvider DependenciesProvider { private get; set; }

        public IStorage SourceStorage { internal get; set; }
        public IStorage VersionsStorage { internal get; set; }
        #endregion

        #region Data
        internal string VPath;
        internal string[] SupportedFormats = { "jpg" };
        internal string[] RawFormats = new string[0];

        private TouchThumbnailPathProvider _vpp;
        #endregion

        #region Public methods
        public IPlugin Install(Config c)
        {
            if (DependenciesProvider == null)
                throw new ConfigurationErrorsException("Missing DependenciesProvider");

            var plugin = DependenciesProvider.Resolve<TouchThumbnail>();

            plugin.SupportedFormats = c.get("touchthumbnail.supportedFormats", string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            plugin.RawFormats = c.get("touchthumbnail.rawFormats", string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            plugin.VPath = c.get("touchthumbnail.prefix", "/");

            plugin.Initialize(c);

            c.Plugins.add_plugin(plugin);
            return this;
        }

        public bool Uninstall(Config c)
        {
            return false;
        }

        private void Initialize(Config c)
        {
            if (_vpp != null) throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");
            _vpp = new TouchThumbnailPathProvider(this);

            c.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender, HttpContext context)
            {
                if (_vpp.IsPathVirtual(context.Request.CurrentExecutionFilePath))
                    c.Pipeline.SkipFileTypeCheck = true;
            };

            c.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, IUrlEventArgs e)
            {
                e.QueryString = new NameValueCollection();

                var file = new TouchThumbnailFile(e.VirtualPath, SourceStorage, _vpp);

                if (!file.IsValid)
                    return;

                if (RawFormats.Contains(file.Extension))
                {
                    e.QueryString = new NameValueCollection
                                        {
                                            {"format", file.Extension},
                                            {"cache", ServerCacheMode.Always.ToString()},
                                            {"process", ProcessWhen.No.ToString()},
                                            {"output", file.GetCacheKey(false)},
                                            {"mime", MimeMapping.GetMimeMapping(file.GetCacheKey(false))}
                                        };
                }
                else if (SupportedFormats.Contains(file.Extension))
                {
                    e.QueryString = new NameValueCollection
                                        {
                                            {"preset", file.Version},
                                            {"format", file.Extension},
                                            {"cache", ServerCacheMode.Always.ToString()},
                                            {"process", ProcessWhen.Always.ToString()},
                                            {"output", file.GetCacheKey(false)},
                                            {"mime", MimeMapping.GetMimeMapping(file.GetCacheKey(false))}
                                        };
                }
            };

            c.Pipeline.PreHandleImage += delegate(IHttpModule sender, HttpContext context, IResponseArgs args)
            {
                args.ResponseHeaders.ContentType = args.RewrittenQuerystring["mime"];
            };

            try
            {
                HostingEnvironment.RegisterVirtualPathProvider(_vpp);
            }
            catch (SecurityException)
            {
                c.configurationSectionIssues.AcceptIssue(new Issue("TouchThumbnail",
                                                                   "TouchThumbnail could not be installed as a VirtualPathProvider due to missing AspNetHostingPermission.",
                                                                   "It was installed as an IVirtualImageProvider instead, which means that only image URLs will be accessible, and only if they contain a querystring.",
                                                                   IssueSeverity.Error));
            }
        }
        #endregion
    }
}
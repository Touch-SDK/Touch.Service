using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Hosting;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Xml;
using System.Collections.Generic;
using Touch.Configuration;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchThumbnail : IPlugin, IMultiInstancePlugin, IRedactDiagnostics
    {
        private readonly string _vpath;
        private readonly string _pathPrefix;
        private readonly string[] _supportedFormats = new[] { "jpg" };
        private readonly string[] _rawFormats = new string[] { "swf", "pdf", "mp3" };
        private readonly Dictionary<string, string> _rawContentTypes = new Dictionary<string, string> 
                                                                        { 
                                                                            { "swf", "application/x-shockwave-flash" },
                                                                            { "pdf", "application/pdf" },
                                                                            { "mp3", "audio/mpeg3" } 
                                                                        };
        private const string DefaultContentType = "application/octet-stream";
        private readonly string[][] _defaults;
        private readonly Bucket _bucket;

        private TouchThumbnailPathProvider _vpp;

        public TouchThumbnail(NameValueCollection args)
        {
            if (!string.IsNullOrEmpty(args["supportedFormats"]))
                _supportedFormats = args["supportedFormats"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (!string.IsNullOrEmpty(args["rawFormats"]))
                _rawFormats = args["rawFormats"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrEmpty(args["prefix"]))
                throw new ConfigurationErrorsException("prefix property is required");

            _vpath = args["prefix"];

            if (string.IsNullOrEmpty(args["connectionString"]))
                throw new ConfigurationErrorsException("connectionString property is required");

            var connectionString = EnvironmentValues.GetConnectionString(args["connectionString"]);

            if (string.IsNullOrEmpty(connectionString))
                throw new ConfigurationErrorsException(connectionString + " connection string is not set.");

            var bucketConfig = new AwsStorageConnectionStringBuilder { ConnectionString = connectionString };
            _pathPrefix = bucketConfig.Path;

            if (string.IsNullOrEmpty(bucketConfig.Region))
                throw new ConfigurationErrorsException("Bucketregion  is not set in connection string " + connectionString);

            _bucket = new Bucket(connectionString, new BasicAWSCredentials(EnvironmentValues.AwsKey, EnvironmentValues.AwsSecret));

            if (!string.IsNullOrEmpty(args["defaults"]))
                _defaults = args["defaults"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split(new[] { '=' })).Take(2).ToArray();
        }

        public IPlugin Install(Config c)
        {
            if (_vpp != null) throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            _vpp = new TouchThumbnailPathProvider
            {
                Bucket = _bucket,
                VirtualFilesystemPrefix = _vpath,
                SupportedFormats = _supportedFormats,
                RawFormats = _rawFormats,
                StoragePath = _pathPrefix
            };

            c.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender, HttpContext context)
            {
                if (_vpp.IsPathVirtual(context.Request.CurrentExecutionFilePath))
                    c.Pipeline.SkipFileTypeCheck = true;
            };

            c.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, IUrlEventArgs e)
            {
                var file = new TouchThumbnailFile(e.VirtualPath, _bucket, _vpp);

                if (file.Exists)
                {
                    if (file.Extension == null || _rawFormats.Contains(file.Extension))
                    {
                        e.QueryString = new NameValueCollection
                                            {
                                                {"cache", ServerCacheMode.Always.ToString()},
                                                {"process", ProcessWhen.No.ToString()}
                                            };

                        if (_rawFormats.Contains(file.Extension))
                            e.QueryString["format"] = file.Extension;
                    }
                    else
                    {
                        e.QueryString = new NameValueCollection
                                            {
                                                {"preset", file.Version},
                                                {"format", file.Extension},
                                                {"cache", ServerCacheMode.Always.ToString()},
                                                {"process", ProcessWhen.Always.ToString()}
                                            };

                        if (_defaults != null)
                        {
                            foreach (var pair in _defaults)
                                e.QueryString[pair[0]] = pair[1];
                        }
                    }
                }
                else
                    e.QueryString = new NameValueCollection();
            };

            c.Pipeline.PreHandleImage += delegate(IHttpModule sender, HttpContext context, IResponseArgs args)
            {
                var hasFormat = args.RewrittenQuerystring.AllKeys.Contains("format");

                if (args.SuggestedExtension == "unknown")
                {
                    if (hasFormat && _rawContentTypes.ContainsKey(args.RewrittenQuerystring["format"]))
                        args.ResponseHeaders.ContentType = _rawContentTypes[args.RewrittenQuerystring["format"]];
                    else
                        args.ResponseHeaders.ContentType = DefaultContentType;
                }
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

            c.Plugins.add_plugin(this);

            return this;
        }

        public bool Uninstall(Config c)
        {
            return false;
        }

        public Node RedactFrom(Node resizer)
        {
            return resizer;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Caching;
using System.Web.Hosting;
using Amazon.S3;
using ImageResizer.Util;

namespace ImageResizer.Plugins.TouchThumbnail
{
    /// <summary>
    /// Allows clients to request objects located on another amazon S3 server through this server. Allows URL rewriting.
    /// </summary>
    sealed public class TouchThumbnailPathProvider : VirtualPathProvider, IVirtualImageProvider
    {
        private string _virtualFilesystemPrefix;

        public string Bucket { get; set; }

        public string[] SupportedFormats { get; set; }

        public string[] RawFormats { get; set; }

        public string StoragePath { get; set; }

        /// <summary>
        /// Requests starting with this path will be handled by this virtual path provider. Should be in app-relative form: "~/thumbnail/".
        /// Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
        /// </summary>
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set
            {
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }

        /// <summary>
        /// Gets and sets the AmazonS3Client object that specifies connection details such as authentication, encryption, etc.
        /// </summary>
        internal AmazonS3Client S3Client { get; set; }

        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            return IsPathVirtual(virtualPath) && new TouchThumbnailFile(virtualPath, Bucket, this).Exists;
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            return (IsPathVirtual(virtualPath)) ? new TouchThumbnailFile(virtualPath, Bucket, this) : null;
        }

        public string FilterPath(string path)
        {
            return path;
        }

        protected override void Initialize()
        {
        }

        public bool IsPathVirtual(string virtualPath)
        {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public override bool FileExists(string virtualPath)
        {
            return IsPathVirtual(virtualPath)
                ? new TouchThumbnailFile(virtualPath, Bucket, this).Exists 
                : Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            return IsPathVirtual(virtualPath)
                ? new TouchThumbnailFile(virtualPath, Bucket, this) 
                : Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return IsPathVirtual(virtualPath) 
                ? new EmptyCacheDependency() 
                : Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        private class EmptyCacheDependency : CacheDependency
        {
        }
    }
}
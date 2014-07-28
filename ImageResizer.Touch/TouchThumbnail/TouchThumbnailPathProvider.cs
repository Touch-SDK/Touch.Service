using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Caching;
using System.Web.Hosting;
using ImageResizer.Util;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchThumbnailPathProvider : VirtualPathProvider, IVirtualImageProvider
    {
        #region .ctor
        public TouchThumbnailPathProvider(TouchThumbnail parent)
        {
            SupportedFormats = parent.SupportedFormats;
            RawFormats = parent.RawFormats;
            VirtualFilesystemPrefix = parent.VPath;

            _bucket = parent.Storage;
        } 
        #endregion

        #region Data
        private readonly IStorage _bucket;

        internal string[] SupportedFormats { get; private set; }
        internal string[] RawFormats { get; private set; } 
        #endregion

        #region Public methods
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set
            {
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }
        private string _virtualFilesystemPrefix;

        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            return IsPathVirtual(virtualPath) && TouchThumbnailFile.IsValid(virtualPath.Substring(VirtualFilesystemPrefix.Length));
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            return (IsPathVirtual(virtualPath)) ? new TouchThumbnailFile(virtualPath, _bucket, this) : null;
        }

        public string FilterPath(string path)
        {
            return path;
        }

        public bool IsPathVirtual(string virtualPath)
        {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public override bool FileExists(string virtualPath)
        {
            return IsPathVirtual(virtualPath)
                ? TouchThumbnailFile.IsValid(virtualPath.Substring(VirtualFilesystemPrefix.Length))
                : Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            return IsPathVirtual(virtualPath)
                ? new TouchThumbnailFile(virtualPath, _bucket, this)
                : Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            return IsPathVirtual(virtualPath)
                ? new EmptyCacheDependency()
                : Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        } 
        #endregion

        #region Helpers
        protected override void Initialize()
        {
        }

        private class EmptyCacheDependency : CacheDependency
        {
        } 
        #endregion
    }
}
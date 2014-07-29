using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
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

            _sourceStorage = parent.SourceStorage;
            _versionsStorage = parent.VersionsStorage;
        } 
        #endregion

        #region Data
        private readonly IStorage _sourceStorage;
        private readonly IStorage _versionsStorage;

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
            return IsPathVirtual(virtualPath) && TouchThumbnailFile.IsValidPath(virtualPath, VirtualFilesystemPrefix);
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (!IsPathVirtual(virtualPath))
                return null;

            return (IVirtualFile)GetFile(virtualPath);
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
                ? TouchThumbnailFile.IsValidPath(virtualPath, VirtualFilesystemPrefix)
                : Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            if (!IsPathVirtual(virtualPath))
                return Previous.GetFile(virtualPath);

            var file = new TouchThumbnailFile(virtualPath, _sourceStorage, this);

            if (file.IsVideo)
            {
                if (!SupportedFormats.Contains(file.Extension))
                    return Previous.GetFile(virtualPath);

                virtualPath = VirtualFilesystemPrefix + file.GetVideoThumbnailKey();
                return new TouchVideoThumbnailFile(virtualPath, _versionsStorage, this);
            }

            return file;
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
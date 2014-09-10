using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using ImageResizer.ExtensionMethods;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchThumbnailFile : VirtualFile, IVirtualFileWithModifiedDate, IVirtualFileSourceCacheKey
    {
        public TouchThumbnailFile(string virtualPath, IStorage bucket, TouchThumbnailPathProvider provider)
            : base(virtualPath)
        {
            if (!provider.IsPathVirtual(virtualPath))
            {
                IsValid = false;
                return;
            }

            _bucket = bucket;

            var path = VirtualPath.Substring(provider.VirtualFilesystemPrefix.Length);
            
            var match = Regex.Match(path, RequestRegex);
            if (!match.Success)
            {
                match = Regex.Match(path, RawRequestRegex);
                if (!match.Success) return;
            }
            else
            {
                Version = match.Groups["version"].Value;
            }

            _cacheKey = match.Value;

            Extension = match.Groups["extension"].Value;

            Guid token;
            if (!Guid.TryParse(match.Groups["token"].Value, out token))
                throw new ArgumentException("Invalid token value: " + match.Groups["token"].Value);
            Token = token;

            IsValid = true;
        }

        #region Data
        private readonly IStorage _bucket;
        private readonly string _cacheKey;

        private const string RequestRegex = @"^(?<token>[a-f0-9-]+)_(?<version>[a-z0-9_-]+)\.(?<extension>[a-z0-9]{1,5})$";
        private const string RawRequestRegex = @"^(?<token>[a-f0-9-]+)(\.(?<extension>[a-z0-9]{1,5}))$";

        private Metadata _Metadata;
        private bool _Exists;
        private bool _IsVideo;
        private bool _metadataFetched;
        #endregion

        #region Properties
        public bool IsValid { get; private set; }
        public bool Exists { get { FetchMetadata(); return _Exists; } }
        public bool IsVideo { get { FetchMetadata(); return _IsVideo; } }
        public Metadata Metadata { get { FetchMetadata(); return _Metadata; } }
        public Guid Token { get; private set; }
        public string Extension { get; private set; }
        public string Version { get; private set; }
        public DateTime ModifiedDateUTC { get { return DateTime.MinValue; } }
        
        #endregion

        #region Public methods
        public static bool IsValidPath(string path, string prefix)
        {
            path = path.Substring(prefix.Length);

            var match = Regex.Match(path, RequestRegex);
            if (match.Success)
                return true;

            match = Regex.Match(path, RawRequestRegex);
            return match.Success;
        }

        public override Stream Open()
        {
            if (!IsValid) throw new InvalidOperationException();
            return _bucket.GetFile(Token.ToString());
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return _cacheKey;
        }

        public string GetVideoThumbnailKey()
        {
            return Token + "-00001.jpg";
        }
        #endregion

        #region Helper methods
        private void FetchMetadata()
        {
            if (_metadataFetched) return;
            _metadataFetched = true;

            Metadata metadata;
            _Exists = _bucket.HasFile(Token.ToString(), out metadata);
            _Metadata = metadata;
            _IsVideo = metadata.ContentType != null && metadata.ContentType.StartsWith("video");
        } 
        #endregion
    }
}
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
                throw new ArgumentException("S3 file path must be located within " + provider.VirtualFilesystemPrefix);

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

            var extension = match.Groups["extension"].Value;
            if (!provider.SupportedFormats.Contains(extension) && !provider.RawFormats.Contains(extension)) return;
            Extension = extension;

            Guid token;
            if (!Guid.TryParse(match.Groups["token"].Value, out token)) return;
            Token = token;

            Metadata metadata;
            Exists = _bucket.HasFile(Token.ToString(), out metadata);
            Metadata = metadata;
        }

        #region Data
        private readonly IStorage _bucket;
        private readonly string _cacheKey;

        private const string RequestRegex = @"^(?<token>[a-f0-9-]+)_(?<version>[a-z0-9_-]+)\.(?<extension>[a-z0-9]{1,5})$";
        private const string RawRequestRegex = @"^(?<token>[a-f0-9-]+)(\.(?<extension>[a-z0-9]{1,5}))$"; 
        #endregion

        #region Properties
        public bool Exists { get; private set; }
        public Guid Token { get; private set; }
        public string Extension { get; private set; }
        public string Version { get; private set; }
        public Metadata Metadata { get; private set; }
        public DateTime ModifiedDateUTC { get { return DateTime.MinValue; } } 
        #endregion

        #region Public methods
        public static bool IsValid(string name)
        {
            var match = Regex.Match(name, RequestRegex);
            if (!match.Success)
            {
                match = Regex.Match(name, RawRequestRegex);
                if (!match.Success) return false;
            }

            return true;
        }

        public override Stream Open()
        {
            if (!Exists) throw new InvalidOperationException();
            return StreamExtensions.CopyToMemoryStream(_bucket.GetFile(Token.ToString()));
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return _cacheKey;
        } 
        #endregion
    }
}
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using Amazon.S3;
using Amazon.S3.Model;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchThumbnailFile : VirtualFile, IVirtualFileWithModifiedDate, IVirtualFileSourceCacheKey
    {
        private readonly string _bucket;
        private readonly string _key;
        private readonly TouchThumbnailPathProvider _provider;

        private const string RequestRegex = @"^(?<key>[a-f0-9-]+)_(?<version>[a-z0-9_-]+)\.(?<extension>[a-z0-9]{1,5})$";
        private const string RawRequestRegex = @"^(?<key>[a-f0-9-]+)(\.(?<extension>[a-z0-9]{1,5}))?$";

        public TouchThumbnailFile(string virtualPath, string bucket, TouchThumbnailPathProvider provider)
            : base(virtualPath)
        {
            _provider = provider;

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

            Guid key;
            if (!Guid.TryParse(match.Groups["key"].Value, out key)) return;
            Key = key;

            var prefix = provider.StoragePath;
            _key = prefix + (!string.IsNullOrWhiteSpace(prefix) ? "/" : string.Empty) + key.ToString().ToLowerInvariant();

            if (match.Groups["extension"].Success)
            {
                var extension = match.Groups["extension"].Value;
                if (!provider.SupportedFormats.Contains(extension) && !provider.RawFormats.Contains(extension)) return;
                Extension = extension;
            }

            Exists = true;
        }

        internal Guid Key { get; private set; }
        internal string Version { get; private set; }
        internal string Extension { get; private set; }

        public bool Exists { get; private set; }
        
        public override Stream Open()
        {
            if (!Exists) throw new InvalidOperationException();

            try
            {
                var req = new GetObjectRequest { BucketName = _bucket, Key = _key };

                using (var s = _provider.S3Client.GetObject(req))
                {
                    return StreamExtensions.CopyToMemoryStream(s.ResponseStream);
                }
            }
            catch (AmazonS3Exception se)
            {
                if (se.StatusCode == HttpStatusCode.NotFound || "NoSuchKey".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Amazon S3 file not found", se);

                if (se.StatusCode == HttpStatusCode.Forbidden || "AccessDenied".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);

                throw;
            }
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return _bucket + Key + Version + Extension;
        }

        public DateTime ModifiedDateUTC
        {
            get
            {
                return DateTime.MinValue;
            }
        }
    }
}
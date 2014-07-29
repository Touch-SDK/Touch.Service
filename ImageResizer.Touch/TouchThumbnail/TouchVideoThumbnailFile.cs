using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using ImageResizer.ExtensionMethods;
using Touch.Storage;

namespace ImageResizer.Plugins.TouchThumbnail
{
    sealed public class TouchVideoThumbnailFile : VirtualFile, IVirtualFileWithModifiedDate, IVirtualFileSourceCacheKey
    {
        public TouchVideoThumbnailFile(string virtualPath, IStorage bucket, TouchThumbnailPathProvider provider)
            : base(virtualPath)
        {
            if (!provider.IsPathVirtual(virtualPath))
                throw new ArgumentException("File path must be located within " + provider.VirtualFilesystemPrefix);

            _bucket = bucket;

            var path = VirtualPath.Substring(provider.VirtualFilesystemPrefix.Length);
            
            var match = Regex.Match(path, RequestRegex);
            if (!match.Success)
                return;

            _key = match.Value;

            Extension = match.Groups["extension"].Value;

            Guid token;
            if (!Guid.TryParse(match.Groups["token"].Value, out token))
                throw new ArgumentException("Invalid token value: " + match.Groups["token"].Value);
            Token = token;

            Sequence = Convert.ToInt16(match.Groups["sequence"].Value);

            IsValid = true;
        }

        #region Data
        private readonly IStorage _bucket;
        private readonly string _key;

        private const string RequestRegex = @"^(?<token>[a-f0-9-]+)-(?<sequence>[0-9]{5})\.(?<extension>[a-z0-9]{1,5})$";
        #endregion

        #region Properties
        public bool IsValid { get; private set; }
        public Guid Token { get; private set; }
        public string Extension { get; private set; }
        public short Sequence { get; private set; }
        public DateTime ModifiedDateUTC { get { return DateTime.MinValue; } } 
        #endregion

        #region Public methods
        public static bool IsValidPath(string path, string prefix)
        {
            path = path.Substring(prefix.Length);
            return Regex.Match(path, RequestRegex).Success;
        }

        public override Stream Open()
        {
            if (!IsValid) throw new InvalidOperationException();
            return StreamExtensions.CopyToMemoryStream(_bucket.GetFile(_key));
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return _key;
        }
        #endregion
    }
}
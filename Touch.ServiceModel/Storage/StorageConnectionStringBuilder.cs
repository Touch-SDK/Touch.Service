using System;
using System.Data.Common;

namespace Touch.Storage
{
    /// <summary>
    /// Storage connection string builder.
    /// </summary>
    sealed public class StorageConnectionStringBuilder : DbConnectionStringBuilder
    {
        #region Properties
        public string Host
        {
            get { return ContainsKey("Host") ? this["Host"] as string : null; }
            set { this["Host"] = value; }
        }

        public string Path
        {
            get { return ContainsKey("Path") ? (string)this["Path"] : string.Empty; }
            set { this["Path"] = value ?? string.Empty; }
        }

        public string Bucket
        {
            get { return ContainsKey("Bucket") ? this["Bucket"] as string : null; }
            set { this["Bucket"] = value; }
        }

        public string Region
        {
            get { return ContainsKey("Region") ? this["Region"] as string : null; }
            set { this["Region"] = value; }
        }

        public bool ReducedRedundancy
        {
            get { return ContainsKey("ReducedRedundancy") && Convert.ToBoolean(this["ReducedRedundancy"]); }
            set { this["ReducedRedundancy"] = value; }
        }

        public string PublicEndpointUrl
        {
            get
            {
                if (Host == null) throw new NotSupportedException("Host name is not available.");

                return "//" + Host + "/" + Path + (Path != string.Empty ? "/" : string.Empty);
            }
        }
        #endregion
    }
}

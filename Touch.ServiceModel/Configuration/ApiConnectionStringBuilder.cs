using System;
using System.Data.Common;

namespace Touch.Configuration
{
    /// <summary>
    /// API connection string builder.
    /// </summary>
    sealed public class ConnectionStringBuilder : DbConnectionStringBuilder
    {
        /// <summary>
        /// Default connection port.
        /// </summary>
        public const int DefaultPort = 80;

        #region Properties
        public string Host
        {
            get { return ContainsKey("Host") ? this["Host"] as string : null; }
            set { this["Host"] = value; }
        }

        public string Path
        {
            get { return ContainsKey("Path") ? this["Path"] as string : null; }
            set { this["Path"] = value; }
        }

        public string Realm
        {
            get { return ContainsKey("Realm") ? this["Realm"] as string : null; }
            set { this["Realm"] = value; }
        }

        public bool IsSecure
        {
            get { return Convert.ToBoolean(this["IsSecure"]); }
            set { this["IsSecure"] = value; }
        }

        public int Port
        {
            get { return ContainsKey("Port") ? Convert.ToInt32(this["Port"]) : DefaultPort; }
            set { this["Port"] = value; }
        }
        #endregion
    }
}

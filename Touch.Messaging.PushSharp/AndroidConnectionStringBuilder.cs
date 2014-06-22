using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touch.Messaging
{
    sealed public class AndroidConnectionStringBuilder : DbConnectionStringBuilder
    {
        #region Properties
        public string ApiKey
        {
            get { return ContainsKey("ApiKey") ? (string)this["ApiKey"] : string.Empty; }
            set { this["ApiKey"] = value ?? string.Empty; }
        }
        #endregion
    }
}

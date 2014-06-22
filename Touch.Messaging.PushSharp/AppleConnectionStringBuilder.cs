using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touch.Messaging
{
    sealed public class AppleConnectionStringBuilder : DbConnectionStringBuilder
    {
        #region Properties
        public string Certificate
        {
            get { return ContainsKey("Certificate") ? (string)this["Certificate"] : string.Empty; }
            set { this["Certificate"] = value ?? string.Empty; }
        }

        public string Password
        {
            get { return ContainsKey("Password") ? (string)this["Password"] : string.Empty; }
            set { this["Password"] = value ?? string.Empty; }
        }

        public bool IsProduction
        {
            get { return ContainsKey("IsProduction") && Convert.ToBoolean(this["IsProduction"]); }
            set { this["IsProduction"] = value; }
        }
        #endregion
    }
}

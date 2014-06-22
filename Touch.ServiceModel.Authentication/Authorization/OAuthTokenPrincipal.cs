using DevDefined.OAuth.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Touch.ServiceModel.Authorization
{
    sealed public class OAuthTokenPrincipal : GenericPrincipal
    {
        readonly TokenBase _token;

        public OAuthTokenPrincipal(IIdentity identity, string[] roles, TokenBase token)
            : base(identity, roles)
        {
            _token = token;
        }

        public TokenBase Token
        {
            get { return _token; }
        }
    }
}

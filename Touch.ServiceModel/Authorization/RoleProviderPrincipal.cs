using System.Security.Principal;
using System.Web.Security;

namespace Touch.ServiceModel
{
    /// <summary>
    /// Role provider-based principal.
    /// </summary>
    sealed public class RoleProviderPrincipal : IPrincipal
    {
        #region .ctor
        public RoleProviderPrincipal(IIdentity identity)
        {
            Identity = identity;
        }
        #endregion

        public IIdentity Identity { get; private set; }

        public bool IsInRole(string role){ return Roles.IsUserInRole(role); } 
    }
}

using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;
using System.Web;

namespace Touch.ServiceModel
{
    /// <summary>
    /// Role provider-based web authorization policy.
    /// </summary>
    sealed public class RoleProviderAuthorizationPolicy : IAuthorizationPolicy
    {
        readonly string _id = Guid.NewGuid().ToString();

        public string Id { get { return _id; } }

        public ClaimSet Issuer { get { return ClaimSet.System; } }

        public bool Evaluate(EvaluationContext evaluationContext, ref object state)
        {
            if (HttpContext.Current == null) return false;

            // get the authenticated client identity
            var client = HttpContext.Current.User.Identity;

            evaluationContext.AddClaimSet(this, new DefaultClaimSet(Claim.CreateNameClaim(client.Name)));
            evaluationContext.Properties["Identities"] = new List<IIdentity>(new[] {client});
            evaluationContext.Properties["Principal"] = new RoleProviderPrincipal(client);

            return true;
        }
    }
}

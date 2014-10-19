using System;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2PrincipalAuthorizationPolicy : IAuthorizationPolicy
    {
        private readonly Guid _uniqueId = Guid.NewGuid();
		private readonly IPrincipal _principal;

		public OAuth2PrincipalAuthorizationPolicy(IPrincipal principal)
        {
			_principal = principal;
		}

		#region IAuthorizationComponent Members
		public string Id 
        {
			get { return _uniqueId.ToString(); }
		}
		#endregion

		#region IAuthorizationPolicy Members
		public ClaimSet Issuer 
        {
			get { return ClaimSet.System; }
		}

		public bool Evaluate(EvaluationContext evaluationContext, ref object state) 
        {
			evaluationContext.AddClaimSet(this, new DefaultClaimSet(Claim.CreateNameClaim(_principal.Identity.Name)));
			evaluationContext.Properties["Principal"] = _principal;
			return true;
		}
		#endregion
    }
}

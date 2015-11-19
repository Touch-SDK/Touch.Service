using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Policy;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Web;
using Touch.Serialization;

namespace Touch.ServiceModel.Authorization
{
    public sealed class DelegatedAuthorizationManager : ServiceAuthorizationManager
    {
        #region Dependencies
        public static Func<DelegatedAuthorizationManager> InstanceProvider { private get; set; }
        public static Func<ISerializer> DeserializerProvider { private get; set; }
        #endregion

        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            if (InstanceProvider == null) throw new ConfigurationErrorsException("InstanceProvider is not set.");
            return InstanceProvider().CheckOAuthAccess(operationContext);
        }

        internal bool CheckOAuthAccess(OperationContext operationContext)
        {
            if (!base.CheckAccessCore(operationContext))
                return false;

            try
            {
                var httpContext = HttpContext.Current;
                var httpDetails = operationContext.RequestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                var requestUri = operationContext.RequestContext.RequestMessage.Properties.Via;

                var req = new HttpRequestWrapper(httpContext.Request);

                if (!req.Headers.AllKeys.Contains("Authorization"))
                {
                    IssueAnonymousPrincipal(operationContext);
                    return true;
                }

                var payload = req.Headers["Authorization"];

                if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith("Bearer "))
                {
                    IssueAnonymousPrincipal(operationContext);
                    return true;
                }

                var encoded = payload.Substring("Bearer ".Length);

                IPrincipal principal;

                try
                {
                    principal = DeserializerProvider().Deserialize<object>(encoded) as IPrincipal;
                }
                catch
                {
                    IssueAnonymousPrincipal(operationContext);
                    return false;
                }

                if (principal != null)
                {
                    var policy = new OAuth2PrincipalAuthorizationPolicy(principal);
                    var policies = new List<IAuthorizationPolicy> { policy };

                    var securityContext = new ServiceSecurityContext(policies.AsReadOnly());

                    if (operationContext.IncomingMessageProperties.Security != null)
                    {
                        operationContext.IncomingMessageProperties.Security.ServiceSecurityContext = securityContext;
                    }
                    else
                    {
                        operationContext.IncomingMessageProperties.Security = new SecurityMessageProperty
                        {
                            ServiceSecurityContext = securityContext
                        };
                    }

                    securityContext.AuthorizationContext.Properties["Principal"] = principal;

                    var identity = principal.Identity;

                    operationContext.IncomingMessageProperties.Security.ServiceSecurityContext = securityContext;
                    securityContext.AuthorizationContext.Properties["Identities"] = new List<IIdentity> { identity };

                    operationContext.IncomingMessageProperties["DelegatedPrincipal"] = principal;

                    return true;
                }

                IssueAnonymousPrincipal(operationContext);
                return true;
            }
            catch (Exception)
            {
                IssueAnonymousPrincipal(operationContext);
                return true;
            }
        }

        private static void IssueAnonymousPrincipal(OperationContext operationContext)
        {
            var securityContext = ServiceSecurityContext.Anonymous;
            var principal = new GenericPrincipal(new GenericIdentity(string.Empty), new string[0]);
            securityContext.AuthorizationContext.Properties["Principal"] = principal;

            var identity = principal.Identity;

            operationContext.IncomingMessageProperties.Security.ServiceSecurityContext = securityContext;
            securityContext.AuthorizationContext.Properties["Identities"] = new List<IIdentity> { identity };
        }
    }
}

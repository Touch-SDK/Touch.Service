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
using DotNetOpenAuth.OAuth2;
using Touch.Domain;
using Touch.Providers;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationManager : ServiceAuthorizationManager
    {
        #region Dependencies
        public static Func<OAuth2AuthorizationManager> InstanceProvider { private get; set; }
        public IOAuth2CryptoService Service { private get; set; }
        public IOAuth2Provider Provider { private get; set; }
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

                IPrincipal principal;
                OAuth2Access access = null;

                using (var signing = Service.GetSigningProvider())
                using (var encrypting = Service.GetEncryptionProvider())
                {
                    var resourceServer = new ResourceServer(new StandardAccessTokenAnalyzer(signing, encrypting));
                    principal = resourceServer.GetPrincipal(httpDetails, requestUri);

                    if (principal != null && httpContext != null)
                    {
                        var accessToken = resourceServer.GetAccessToken(new HttpRequestWrapper(httpContext.Request));
                        
                        if (accessToken != null)
                        {                            
                            access = new OAuth2Access
                            {
                                HashKey = accessToken.User,
                                ClientId = accessToken.ClientIdentifier,
                                Roles = accessToken.Scope.ToArray(),
                                IssueDate = accessToken.UtcIssued.ToDocumentString()
                            };

                            foreach (var pair in accessToken.ExtraData)
                                access.ExtraData[pair.Key] = pair.Value;
                        }
                    }
                }

                if (access != null)
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

                    Provider.ActiveAccess = access;

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

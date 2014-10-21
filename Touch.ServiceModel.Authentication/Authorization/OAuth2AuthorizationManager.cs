using System;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Web;
using DotNetOpenAuth.OAuth2;
using Touch.Domain;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationManager : ServiceAuthorizationManager
    {
        #region .ctor
        public OAuth2AuthorizationManager()
        {
            Service = CryptoService;
        } 
        #endregion

        #region Dependencies
        public static IOAuth2CryptoService CryptoService { private get; set; }
        public IOAuth2CryptoService Service { private get; set; }
        #endregion

        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            if (!base.CheckAccessCore(operationContext))
                return false;

            try
            {
                var httpContext = HttpContext.Current;
                var httpDetails = operationContext.RequestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                var requestUri = operationContext.RequestContext.RequestMessage.Properties.Via;

                IPrincipal principal;
                OAuth2User user = null;

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
                            user = new OAuth2User
                            {
                                ClientId = accessToken.ClientIdentifier,
                                UserName = accessToken.User,
                                Roles = accessToken.Scope.ToArray()
                            };

                            if (!accessToken.ExtraData.ContainsKey("oauth_user_token"))
                                throw new OperationCanceledException("User token not found.");
                            
                            user.HashKey = accessToken.ExtraData["oauth_user_token"];
                        }
                    }
                }
                
                if (principal != null)
                {
                    var policy = new OAuth2PrincipalAuthorizationPolicy(principal);
                    var policies = new List<IAuthorizationPolicy> {policy};

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

                    if (user != null)
                        operationContext.IncomingMessageProperties["OAuth2User"] = user;

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

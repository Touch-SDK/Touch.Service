using System;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Web;
using Touch.Domain;
using Touch.ServiceModel.OAuth;
using DevDefined.OAuth.Framework;
using AccessToken = DevDefined.OAuth.Storage.Basic.AccessToken;

namespace Touch.ServiceModel.Authorization
{
    sealed public class OAuthAuthorizationManager : ServiceAuthorizationManager
    {
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            if (!base.CheckAccessCore(operationContext))
                return false;

            try
            {
                var oAuthProvider = OAuthServicesLocator.Current.OAuthProvider;
                var tokenRepository = OAuthServicesLocator.Current.TokenStore;

                var requestMessage = operationContext.RequestContext.RequestMessage;
                var context = WebOperationContext.Current;

                string host = null;

                if (context != null)
                {
                    host = context.IncomingRequest.Headers.Get("Host");

                    var position = host.IndexOf(':');

                    if (position != -1)
                        host = host.Substring(0, position);
                }

                var oAuthContext = new OAuthContextBuilder().FromWcfMessage(requestMessage, host);

                OperationContext.Current.IncomingMessageProperties["OAuthContext"] = oAuthContext;

                if (!oAuthContext.UseAuthorizationHeader || oAuthContext.Token == null || !string.IsNullOrEmpty(oAuthContext.Verifier))
                {
                    IssueAnonymousPrincipal(operationContext);
                    return true;
                }

                oAuthProvider.AccessProtectedResourceRequest(oAuthContext);

                var accessToken = tokenRepository.GetAccessToken(oAuthContext.Token, oAuthContext.ConsumerKey);
                var principal = CreatePrincipalFromToken(accessToken);

                var consumerDescriptor = new OAuthCredentials
                {
                    Consumer = new OAuthKeyPair { Key = accessToken.ConsumerKey },
                    Token = new OAuthKeyPair(accessToken.Token, accessToken.TokenSecret)
                };

                OperationContext.Current.IncomingMessageProperties["OAuthUser"] = consumerDescriptor;

                InitializeSecurityContext(requestMessage, principal);
            }
            catch
            {
                IssueAnonymousPrincipal(operationContext);
                return true;
            }

            return true;
        }

        static void IssueAnonymousPrincipal(OperationContext operationContext)
        {
            var securityContext = ServiceSecurityContext.Anonymous;
            var principal = new GenericPrincipal(new GenericIdentity(string.Empty), new string[0]);
            securityContext.AuthorizationContext.Properties["Principal"] = principal;

            var identity = principal.Identity;

            operationContext.IncomingMessageProperties.Security.ServiceSecurityContext = securityContext;
            securityContext.AuthorizationContext.Properties["Identities"] = new List<IIdentity> { identity };
        }

        static OAuthTokenPrincipal CreatePrincipalFromToken(AccessToken accessToken)
        {
            return new OAuthTokenPrincipal(
              new GenericIdentity(accessToken.ConsumerKey, "OAuth"),
              accessToken.Roles,
              accessToken);
        }

        static void InitializeSecurityContext(Message request, IPrincipal principal)
        {
            var policies = new List<IAuthorizationPolicy> { new PrincipalAuthorizationPolicy(principal) };
            var securityContext = new ServiceSecurityContext(policies.AsReadOnly());

            if (request.Properties.Security != null)
            {
                request.Properties.Security.ServiceSecurityContext = securityContext;
            }
            else
            {
                request.Properties.Security = new SecurityMessageProperty { ServiceSecurityContext = securityContext };
            }
        }
    }
}

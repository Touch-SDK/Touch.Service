using System;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Web;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Touch.ServiceModel.OAuth;
using ProtocolException = System.ServiceModel.ProtocolException;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationManager : ServiceAuthorizationManager
    {
        public OAuth2AuthorizationManager()
        {
            return;
        }

        #region Dependencies
        public IOAuth2CryptoService CryptoService { private get; set; }
        #endregion

        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            if (!base.CheckAccessCore(operationContext))
            {
                return false;
            }

            var httpDetails = operationContext.RequestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
            var requestUri = operationContext.RequestContext.RequestMessage.Properties.Via;
            
            try
            {
                var principal = VerifyOAuth2(httpDetails, requestUri, operationContext.IncomingMessageHeaders.Action ?? operationContext.IncomingMessageHeaders.To.AbsolutePath);
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
                            ServiceSecurityContext = securityContext,
                        };
                    }

                    securityContext.AuthorizationContext.Properties["Identities"] = new List<IIdentity> { principal.Identity };

                    return true;
                }

                return false;
            }
            catch (ProtocolFaultResponseException ex)
            {
                // Return the appropriate unauthorized response to the client.
                var outgoingResponse = ex.CreateErrorResponse();
                outgoingResponse.Respond(WebOperationContext.Current.OutgoingResponse);

                throw;
            }
            catch (ProtocolException ex)
            {
                throw;
            }

            return false;
        }

        private IPrincipal VerifyOAuth2(HttpRequestMessageProperty httpDetails, Uri requestUri, params string[] requiredScopes)
        {
            using (var signing = CryptoService.GetSigningProvider())
            using (var encrypting = CryptoService.GetEncryptionProvider())
            {
                var resourceServer = new ResourceServer(new StandardAccessTokenAnalyzer(signing, encrypting));
                return resourceServer.GetPrincipal(httpDetails, requestUri, requiredScopes);
            }
        }
    }
}

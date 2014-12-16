using System;
using System.Collections.Generic;
using System.Linq;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.ChannelElements;
using DotNetOpenAuth.OAuth2.Messages;
using Touch.Domain;
using Touch.Providers;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationServer : IAuthorizationServerHost
    {
        #region .ctor
        public OAuth2AuthorizationServer()
        {
            AccessTokenLifeSpan = TimeSpan.MaxValue;
            UseRefreshTokens = false;
        }
        #endregion

        #region Dependencies
        public IOAuth2CryptoService CryptoService { private get; set; }
        public ICryptoKeyStore CryptoKeyStore { get; set; }
        public INonceStore NonceStore { get; set; }
        public IOAuth2Manager Provider { private get; set; }
        #endregion

        #region Configuration
        public TimeSpan AccessTokenLifeSpan { get; set; }
        public bool UseRefreshTokens { get; set; }
        #endregion

        #region Implementation of IAuthorizationServerHost
        public AutomatedAuthorizationCheckResponse CheckAuthorizeClientCredentialsGrant(IAccessTokenRequest accessRequest)
        {
            throw new NotSupportedException();
        }

        public AccessTokenResult CreateAccessToken(IAccessTokenRequest accessTokenRequestMessage)
        {
            var accessToken = new AuthorizationServerAccessToken
            {
                ResourceServerEncryptionKey = CryptoService.GetEncryptionProvider(),
                AccessTokenSigningKey = CryptoService.GetSigningProvider(),
                ClientIdentifier = accessTokenRequestMessage.ClientIdentifier
            };

            if (AccessTokenLifeSpan < TimeSpan.MaxValue)
                accessToken.Lifetime = AccessTokenLifeSpan;

            return new AccessTokenResult(accessToken) { AllowRefreshToken = UseRefreshTokens };
        }

        public IClientDescription GetClient(string clientIdentifier)
        {
            var client = Provider.GetClient(clientIdentifier);

            if (client == null)
                throw new ArgumentOutOfRangeException("clientIdentifier");

            return new OAuth2ClientDescription(client);
        }

        public bool IsAuthorizationValid(IAuthorizationDescription authorization)
        {
            var access = Provider.GetAccess(authorization.User, authorization.ClientIdentifier);

            if (access == null) 
                return false;

            var issueDate = access.IssueDate.FromDocumentString() + TimeSpan.FromSeconds(5);

            if (authorization.UtcIssued > issueDate)
                return false;

            if (!authorization.Scope.Any())
                return true;

            var client = Provider.GetClient(authorization.ClientIdentifier);

            var result = authorization.Scope.IsSubsetOf(client.Roles);
            return result;
        }

        public AutomatedUserAuthorizationCheckResponse CheckAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest)
        {
            AutomatedUserAuthorizationCheckResponse response;

            if (Provider.ValidateUserCredentials(userName, password, accessRequest.ClientIdentifier))
            {
                var access = Provider.GrantAccess(userName, accessRequest.ClientIdentifier);

                response = new AutomatedUserAuthorizationCheckResponse(accessRequest, true, access.HashKey);

                var scope = new HashSet<string>(access.Roles);

                if (accessRequest.Scope.Count > 0)
                    scope.IntersectWith(accessRequest.Scope);

                foreach (var s in scope)
                    response.ApprovedScope.Add(s);
            }
            else
            {
                response = new AutomatedUserAuthorizationCheckResponse(accessRequest, false, null);
            }

            return response;
        }
        #endregion
    }
}

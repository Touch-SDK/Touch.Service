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
            
            OAuth2User user;

            if (!string.IsNullOrEmpty(accessTokenRequestMessage.UserName))
            {
                user = Provider.GetUserByUsername(accessTokenRequestMessage.UserName, accessTokenRequestMessage.ClientIdentifier);
                if (user == null) throw new ArgumentOutOfRangeException("username", "User not found.");
            }
            else if (accessToken.ExtraData.ContainsKey("ticket_id"))
            {
                var token = accessToken.ExtraData["ticket_id"];
                user = Provider.GetUserByTicket(token, accessTokenRequestMessage.ClientIdentifier);
                if (user == null) throw new ArgumentOutOfRangeException("ticket_id", "Ticket not found.");

                Provider.ConsumeUserTicket(token, accessTokenRequestMessage.ClientIdentifier);
            }
            else
            {
                throw new NotSupportedException("Unknown authorization workflow.");
            }

            accessToken.User = user.UserName;

            var roles = accessTokenRequestMessage.Scope != null && accessTokenRequestMessage.Scope.Count > 0
                ? user.Roles.Intersect(accessTokenRequestMessage.Scope)
                : user.Roles;

            foreach (var scope in roles)
                accessToken.Scope.Add(scope);

            accessToken.ExtraData.Clear();
            accessToken.ExtraData["oauth_user_token"] = user.HashKey;
            accessToken.ExtraData["oauth_security_token"] = user.SecurityToken;

            var result = new AccessTokenResult(accessToken) { AllowRefreshToken = UseRefreshTokens };
            return result;
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
            return false;
        }

        public AutomatedUserAuthorizationCheckResponse CheckAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest)
        {
            AutomatedUserAuthorizationCheckResponse response;

            if (Provider.ValidateUserCredentials(userName, password, accessRequest.ClientIdentifier))
            {
                var user = Provider.GetUserByUsername(userName, accessRequest.ClientIdentifier);

                response = new AutomatedUserAuthorizationCheckResponse(accessRequest, true, userName);

                var scope = new HashSet<string>(user.Roles);

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

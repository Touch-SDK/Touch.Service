using System;
using System.Collections.Generic;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.ChannelElements;
using DotNetOpenAuth.OAuth2.Messages;
using Touch.Logic;
using Touch.Providers;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationServer : IAuthorizationServerHost
    {
        #region .ctor
        public OAuth2AuthorizationServer()
        {
            AccessTokenLifeSpan = TimeSpan.FromDays(1);
            UseRefreshTokens = true;
        }
        #endregion

        #region Dependencies
        public IOAuth2CryptoService CryptoService { private get; set; }
        public ICryptoKeyStore CryptoKeyStore { get; set; }
        public INonceStore NonceStore { get; set; }
        public OAuth2Logic Logic { private get; set; }
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
                Lifetime = AccessTokenLifeSpan,
                ResourceServerEncryptionKey = CryptoService.GetEncryptionProvider(),
                AccessTokenSigningKey = CryptoService.GetSigningProvider(),
                ClientIdentifier = accessTokenRequestMessage.ClientIdentifier,
                User = accessTokenRequestMessage.UserName
            };

            foreach (var scope in accessTokenRequestMessage.Scope)
                accessToken.Scope.Add(scope);

            var user = Provider.GetUser(accessTokenRequestMessage.UserName, accessTokenRequestMessage.ClientIdentifier);
            if (user == null) throw new OperationCanceledException("User not found.");

            accessToken.ExtraData["oauth_user_token"] = user.HashKey;
            accessToken.ExtraData["oauth_security_token"] = user.SecurityToken;

            var result = new AccessTokenResult(accessToken) { AllowRefreshToken = UseRefreshTokens };
            return result;
        }

        public IClientDescription GetClient(string clientIdentifier)
        {
            var client = Logic.GetClient(clientIdentifier);

            if (client == null)
                throw new ArgumentOutOfRangeException("clientIdentifier");

            return new OAuth2ClientDescription(client);
        }

        public bool IsAuthorizationValid(IAuthorizationDescription authorization)
        {
            return false;
            //return IsAuthorizationValid(authorization.Scope, authorization.ClientIdentifier, authorization.UtcIssued, authorization.User);
        }

        public AutomatedUserAuthorizationCheckResponse CheckAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest)
        {
            AutomatedUserAuthorizationCheckResponse response;

            if (Provider.ValidateUserCredentials(userName, password, accessRequest.ClientIdentifier))
            {
                var user = Provider.GetUser(userName, accessRequest.ClientIdentifier);

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

        /*public bool CanBeAutoApproved(EndUserAuthorizationRequest authorizationRequest)
        {
            if (authorizationRequest == null) throw new ArgumentNullException("authorizationRequest");

            // NEVER issue an auto-approval to a client that would end up getting an access token immediately
            // (without a client secret), as that would allow arbitrary clients to masquarade as an approved client
            // and obtain unauthorized access to user data.
            if (authorizationRequest.ResponseType == EndUserAuthorizationResponseType.AuthorizationCode)
            {
                // Never issue auto-approval if the client secret is blank, since that too makes it easy to spoof
                // a client's identity and obtain unauthorized access.
                var requestingClient = MvcApplication.DataContext.Clients.First(c => c.ClientIdentifier == authorizationRequest.ClientIdentifier);
                if (!string.IsNullOrEmpty(requestingClient.ClientSecret))
                {
                    return this.IsAuthorizationValid(
                        authorizationRequest.Scope,
                        authorizationRequest.ClientIdentifier,
                        DateTime.UtcNow,
                        HttpContext.Current.User.Identity.Name);
                }
            }

            // Default to not auto-approving.
            return false;
        }*/

        /*private bool IsAuthorizationValid(HashSet<string> requestedScopes, string clientIdentifier, DateTime issuedUtc, string username)
        {
            // If db precision exceeds token time precision (which is common), the following query would
            // often disregard a token that is minted immediately after the authorization record is stored in the db.
            // To compensate for this, we'll increase the timestamp on the token's issue date by 1 second.
            issuedUtc += TimeSpan.FromSeconds(1);
            var grantedScopeStrings = from auth in MvcApplication.DataContext.ClientAuthorizations
                                      where
                                          auth.Client.ClientIdentifier == clientIdentifier &&
                                          auth.CreatedOnUtc <= issuedUtc &&
                                          (!auth.ExpirationDateUtc.HasValue || auth.ExpirationDateUtc.Value >= DateTime.UtcNow) &&
                                          auth.User.OpenIDClaimedIdentifier == username
                                      select auth.Scope;

            if (!grantedScopeStrings.Any())
            {
                // No granted authorizations prior to the issuance of this token, so it must have been revoked.
                // Even if later authorizations restore this client's ability to call in, we can't allow
                // access tokens issued before the re-authorization because the revoked authorization should
                // effectively and permanently revoke all access and refresh tokens.
                return false;
            }

            var grantedScopes = new HashSet<string>(OAuthUtilities.ScopeStringComparer);

            foreach (string scope in grantedScopeStrings)
                grantedScopes.UnionWith(OAuthUtilities.SplitScopes(scope));

            return requestedScopes.IsSubsetOf(grantedScopes);
        }*/
    }
}

using System;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.ChannelElements;
using DotNetOpenAuth.OAuth2.Messages;
using Touch.Logic;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Authorization
{
    public sealed class OAuth2AuthorizationServer : IAuthorizationServerHost
    {
        #region .ctor
        public OAuth2AuthorizationServer()
        {
            AccessTokenLifeSpan = TimeSpan.FromDays(1);
        }
        #endregion

        #region Dependencies
        public IOAuth2CryptoService CryptoService { private get; set; }
        public OAuth2KeyStore KeyStore { private get; set; }
        public OAuth2Logic Logic { private get; set; }
        #endregion

        #region Configuration
        public TimeSpan AccessTokenLifeSpan { get; set; }
        #endregion

        #region Implementation of IAuthorizationServerHost
        public ICryptoKeyStore CryptoKeyStore
        {
            get { return KeyStore; }
        }

        public INonceStore NonceStore
        {
            get { return KeyStore; }
        }

        public AutomatedAuthorizationCheckResponse CheckAuthorizeClientCredentialsGrant(IAccessTokenRequest accessRequest)
        {
            throw new NotSupportedException();
        }

        //public bool TryAuthorizeClientCredentialsGrant(IAccessTokenRequest accessRequest)
        //{
        //    throw new NotSupportedException();
        //}

        /*public bool TryAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest, out string canonicalUserName)
        {
            canonicalUserName = userName;

            throw new NotSupportedException();
        }*/

        public AccessTokenResult CreateAccessToken(IAccessTokenRequest accessTokenRequestMessage)
        {
            var accessToken = new AuthorizationServerAccessToken
            {
                Lifetime = AccessTokenLifeSpan,
                ResourceServerEncryptionKey = CryptoService.GetEncryptionProvider(),
                AccessTokenSigningKey = CryptoService.GetSigningProvider()
            };

            var result = new AccessTokenResult(accessToken);
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
            var approved = true;
            return new AutomatedUserAuthorizationCheckResponse(accessRequest, approved, userName);
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

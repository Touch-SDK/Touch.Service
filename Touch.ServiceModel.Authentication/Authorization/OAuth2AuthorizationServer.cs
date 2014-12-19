using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Reflection;
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
        #region Dependencies
        public IOAuth2CryptoService CryptoService { private get; set; }
        public ICryptoKeyStore CryptoKeyStore { get; set; }
        public INonceStore NonceStore { get; set; }
        public IOAuth2Manager Manager { private get; set; }
        #endregion

        #region Implementation of IAuthorizationServerHost
        public AutomatedAuthorizationCheckResponse CheckAuthorizeClientCredentialsGrant(IAccessTokenRequest accessRequest)
        {
            throw new NotSupportedException();
        }

        public AccessTokenResult CreateAccessToken(IAccessTokenRequest accessTokenRequestMessage)
        {
            if (CryptoService == null) throw new ConfigurationErrorsException("CryptoService was not provided.");
            if (Manager == null) throw new ConfigurationErrorsException("Manager was not provided.");

            var accessToken = new AuthorizationServerAccessToken
            {
                ResourceServerEncryptionKey = CryptoService.GetEncryptionProvider(),
                AccessTokenSigningKey = CryptoService.GetSigningProvider(),
                ClientIdentifier = accessTokenRequestMessage.ClientIdentifier
            };

            var client = Manager.GetClient(accessTokenRequestMessage.ClientIdentifier);
            if (client == null) throw new ArgumentOutOfRangeException();

            var accessId = accessTokenRequestMessage.UserName ?? GetUserFromAccessTokenRequest(accessTokenRequestMessage);
            if (accessId == null) throw new IndexOutOfRangeException("Missing access ID.");

            var access = Manager.GetAccess(accessId, accessTokenRequestMessage.ClientIdentifier);
            if (access == null) throw new IndexOutOfRangeException("Access ID not found.");

            foreach (var pair in access.ExtraData)
                accessToken.ExtraData[pair.Key] = pair.Value;

            var allowRefreshToken = !client.IsPublic;

            if (allowRefreshToken)
            {
                var lifetime = Manager.GetAccessLifeSpan(access);

                if (lifetime < TimeSpan.MaxValue)
                    accessToken.Lifetime = lifetime;
                else
                    allowRefreshToken = false;
            }

            return new AccessTokenResult(accessToken) { AllowRefreshToken = allowRefreshToken };
        }

        public IClientDescription GetClient(string clientIdentifier)
        {
            if (Manager == null) throw new ConfigurationErrorsException("Manager was not provided.");

            var client = Manager.GetClient(clientIdentifier);

            if (client == null)
                throw new ArgumentOutOfRangeException("clientIdentifier");

            return new OAuth2ClientDescription(client);
        }

        public bool IsAuthorizationValid(IAuthorizationDescription authorization)
        {
            if (Manager == null) throw new ConfigurationErrorsException("Manager was not provided.");

            var access = Manager.GetAccess(authorization.User, authorization.ClientIdentifier);

            if (access == null) 
                return false;

            var issueDate = access.IssueDate.FromDocumentString() + TimeSpan.FromMinutes(1);

            #if DEBUG
            issueDate += TimeSpan.FromMinutes(9);
            #endif

            if (authorization.UtcIssued > issueDate)
                return false;

            var accessLifeSpan = Manager.GetAccessLifeSpan(access);
            var expirationDate = accessLifeSpan < TimeSpan.MaxValue
                ? issueDate + accessLifeSpan
                : DateTime.MaxValue;

            if (expirationDate < DateTime.UtcNow)
                return false;

            if (!authorization.Scope.Any())
                return true;

            var result = authorization.Scope.IsSubsetOf(access.Roles);
            return result;
        }

        public AutomatedUserAuthorizationCheckResponse CheckAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest)
        {
            if (Manager == null) throw new ConfigurationErrorsException("Manager was not provided.");

            AutomatedUserAuthorizationCheckResponse response;

            if (Manager.ValidateUserCredentials(userName, password, accessRequest.ClientIdentifier))
            {
                var access = Manager.GrantAccess(userName, accessRequest.ClientIdentifier);

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

        #region Helper methods
        private static string GetUserFromAccessTokenRequest(IAccessTokenRequest accessTokenRequest)
        {
            var authorizationDescription = accessTokenRequest as IAuthorizationDescription;
            if (authorizationDescription != null) return authorizationDescription.User;

            var accessTokenRequestBase = accessTokenRequest as AccessTokenRequestBase;
            if (accessTokenRequestBase == null) return null;

            return (from p in accessTokenRequest.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                    where typeof(IAuthorizationDescription).IsAssignableFrom(p.PropertyType)
                    select p.GetValue(accessTokenRequest) as IAuthorizationDescription)
                    .Where(x => x != null)
                    .Select(x => x.User)
                    .FirstOrDefault();
        }
        #endregion
    }
}

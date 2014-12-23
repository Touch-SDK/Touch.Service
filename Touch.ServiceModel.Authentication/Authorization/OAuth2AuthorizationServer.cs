using System;
using System.Collections.Generic;
using System.Configuration;
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

            accessToken.Lifetime = client.AccessWindow > 0
                ? TimeSpan.FromSeconds(client.AccessWindow)
                : TimeSpan.MaxValue;

            return new AccessTokenResult(accessToken) { AllowRefreshToken = !client.IsPublic };
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

            var client = Manager.GetClient(access.ClientId);
            if (client == null) 
                return false;

            var issueDate = access.IssueDate.FromDocumentString() + TimeSpan.FromMinutes(10);

            if (client.IsPublic && authorization.UtcIssued > issueDate)
                return false;

            if (authorization.Scope.Any() && !authorization.Scope.IsSubsetOf(access.Roles))
                return false;

            return true;
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

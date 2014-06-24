using System;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage;
using DevDefined.OAuth.Utility;
using DevDefined.OAuth.Consumer;
using Touch.Domain;
using Touch.Logic;
using AccessToken = DevDefined.OAuth.Storage.Basic.AccessToken;
using RequestToken = DevDefined.OAuth.Storage.Basic.RequestToken;

namespace Touch.ServiceModel.OAuth
{
    sealed public class OAuthTokenStore : ITokenStore
    {
        #region Dependencies
        public AuthenticationLogic AuthenticationLogic { private get; set; }
        #endregion

        #region Public methods
        public AccessToken GetAccessToken(string token, string consumer)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException("token");
            if (string.IsNullOrEmpty(consumer)) throw new ArgumentNullException("consumer");

            var accessToken = AuthenticationLogic.GetAccessToken(token);
            if (accessToken == null) throw new ArgumentException("Access token not found.", "token");

            if (accessToken.Consumer != consumer) throw new ArgumentException("Invalid consumer key.", "consumer");

            return new AccessToken
            {
                ConsumerKey = accessToken.Consumer,
                ExpiryDate = accessToken.ExpirationDate.FromDocumentString(),
                Token = accessToken.HashKey,
                TokenSecret = accessToken.Secret,
                Roles = accessToken.Roles.Split(',')
            };
        }
        #endregion

        #region ITokenStore implementation
        public IToken CreateRequestToken(IOAuthContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (string.IsNullOrEmpty(context.ConsumerKey))
                throw new OAuthException(context, OAuthProblems.ParameterAbsent, "Consumer key is not provided.");

            try
            {
                var entity = AuthenticationLogic.IssueRequestToken(context.ConsumerKey, context.Realm, context.CallbackUrl);

                var token = new RequestToken
                {
                    ConsumerKey = context.ConsumerKey,
                    Realm = context.Realm,
                    Token = entity.HashKey,
                    TokenSecret = entity.Secret,
                    CallbackUrl = context.CallbackUrl,
                    Verifier = entity.Verification
                };

                return token;
            }
            catch (Exception e)
            {
                throw new OAuthException(context, OAuthProblems.PermissionDenied, "Access denied.", e);
            }
        }

        /// <summary>
        /// Create an access token using xAuth.
        /// </summary>
        public IToken CreateAccessToken(IOAuthContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (string.IsNullOrEmpty(context.XAuthUsername))
                throw new OAuthException(context, OAuthProblems.ParameterAbsent, "Username is not provided.");

            if (string.IsNullOrEmpty(context.Realm))
                throw new OAuthException(context, OAuthProblems.ParameterAbsent, "Realm is not provided.");

            try
            {
                var token = AuthenticationLogic.GrantApiAccess(context.ConsumerKey);

                return new AccessToken
                {
                    Token = token.HashKey,
                    TokenSecret = token.Secret,
                    ConsumerKey = context.ConsumerKey,
                    Realm = context.Realm,
                    ExpiryDate = token.ExpirationDate.FromDocumentString(),
                    Roles = token.Roles.Split(','),
                    UserName = context.XAuthUsername
                };
            }
            catch (ArgumentException e)
            {
                switch (e.ParamName)
                {
                    case "username":
                        throw new OAuthException(context, OAuthProblems.PermissionDenied, string.Format("User '{0}' not found.", context.XAuthUsername), e);

                    case "consumerKey":
                        throw new OAuthException(context, OAuthProblems.PermissionDenied, "Consumer key is invalid.", e);

                    default:
                        throw new OAuthException(context, OAuthProblems.PermissionDenied, "Access denied.", e);
                }
            }
            catch (Exception e)
            {
                throw new OAuthException(context, OAuthProblems.PermissionDenied, "Access denied.", e);
            }
        }

        public void ConsumeRequestToken(IOAuthContext requestContext)
        {
            if (requestContext == null) throw new ArgumentNullException("requestContext");

            var requestToken = GetRequestToken(requestContext);

            if (requestToken.UsedUp)
                throw new OAuthException(requestContext, OAuthProblems.TokenRejected, "The request token has already be consumed.");

            if (!AuthenticationLogic.ConsumeRequestToken(requestToken.Token))
                throw new OAuthException(requestContext, OAuthProblems.TokenRejected, "There was a problem consuming request token.");
        }

        public void ConsumeAccessToken(IOAuthContext accessContext)
        {
            var accessToken = GetAccessToken(accessContext);

            if (accessToken.ExpiryDate < Clock.Now)
                throw new OAuthException(accessContext, OAuthProblems.TokenExpired, "Token has expired.");
            
            if (!AuthenticationLogic.ConsumeAccessToken(accessToken.Token))
                throw new OAuthException(accessContext, OAuthProblems.TokenRejected, "There was a problem consuming access token.");
        }

        public IToken GetAccessTokenAssociatedWithRequestToken(IOAuthContext requestContext)
        {
            return GetRequestToken(requestContext, true).AccessToken;
        }

        public RequestForAccessStatus GetStatusOfRequestForAccess(IOAuthContext accessContext)
        {
            var request = GetRequestToken(accessContext, true);

            if (request.AccessDenied) return RequestForAccessStatus.Denied;
            if (request.AccessToken == null) return RequestForAccessStatus.Unknown;

            return RequestForAccessStatus.Granted;
        }

        public string GetCallbackUrlForToken(IOAuthContext requestContext)
        {
            return GetRequestToken(requestContext).CallbackUrl;
        }

        public string GetVerificationCodeForRequestToken(IOAuthContext requestContext)
        {
            return GetRequestToken(requestContext).Verifier;
        }

        public string GetRequestTokenSecret(IOAuthContext requestContext)
        {
            return GetRequestToken(requestContext).TokenSecret;
        }

        public string GetAccessTokenSecret(IOAuthContext requestContext)
        {
            return GetAccessToken(requestContext).TokenSecret;
        }

        public IToken RenewAccessToken(IOAuthContext requestContext)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Helper methods
        private RequestToken GetRequestToken(IOAuthContext context, bool prefetchAccessToken = false)
        {
            try
            {
                var requestToken = AuthenticationLogic.GetRequestToken(context.Token);
                if (requestToken == null) throw new Exception("Request token not found: " + context.Token);

                var result = new RequestToken
                {
                    AccessDenied = string.Compare(requestToken.Status, "Denied", true) == 0,
                    UsedUp = string.Compare(requestToken.Status, "UsedUp", true) == 0,
                    ConsumerKey = requestToken.Consumer,
                    Token = requestToken.HashKey,
                    TokenSecret = requestToken.Secret,
                    Verifier = requestToken.Verification,
                    Realm = requestToken.Realm,
                    CallbackUrl = "oob"
                };

                if (prefetchAccessToken && !string.IsNullOrWhiteSpace(requestToken.AccessToken))
                {
                    var accessToken = AuthenticationLogic.GetAccessToken(requestToken.AccessToken);
                    if (accessToken == null) throw new Exception("Access token for request token not found: " + context.Token);

                    result.AccessToken = new AccessToken
                    {
                        ConsumerKey = accessToken.Consumer,
                        ExpiryDate = accessToken.ExpirationDate.FromDocumentString(),
                        Token = accessToken.HashKey,
                        TokenSecret = accessToken.Secret,
                        Roles = accessToken.Roles.Split(',')
                    };
                }

                return result;
            }
            catch (Exception e)
            {
                throw Error.UnknownToken(context, context.Token, e);
            }
        }

        private AccessToken GetAccessToken(IOAuthContext context)
        {
            try
            {
                var accessToken = AuthenticationLogic.GetAccessToken(context.Token);
                if (accessToken == null) throw new Exception("Access token not found: " + context.Token);

                return new AccessToken
                {
                    ConsumerKey = accessToken.Consumer,
                    ExpiryDate = accessToken.ExpirationDate.FromDocumentString(),
                    Token = accessToken.HashKey,
                    TokenSecret = accessToken.Secret,
                    Roles = accessToken.Roles.Split(',')
                };
            }
            catch (Exception e)
            {
                throw Error.UnknownToken(context, context.Token, e);
            }
        }
        #endregion
    }
}

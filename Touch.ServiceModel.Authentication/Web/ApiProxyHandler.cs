using System;
using System.IO;
using System.Net;
using System.Web;
using DevDefined.OAuth;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using Touch.Configuration;
using Touch.Logging;
using Touch.Providers;
using Touch.ServiceModel.OAuth;

namespace Touch.ServiceModel.Web
{
    sealed public class ApiProxyHandler : IHttpHandler
    {
        #region .ctor
        public ApiProxyHandler()
        {
            if (Instance == null)
                throw new InvalidOperationException("Instance is not provided.");
        }

        public ApiProxyHandler(string connectionString)
        {
            var configBuilder = new ConnectionStringBuilder { ConnectionString = connectionString };

            var urlBuilder = new UriBuilder
            {
                Scheme = configBuilder.IsSecure ? "https" : "http",
                Host = configBuilder.Host,
                Port = configBuilder.Port
            };

            _apiUrl = urlBuilder.Uri;
        }
        #endregion

        #region Dependencies
        public IAuthenticationProvider AuthenticationProvider { private get; set; }
        public ILoggerProvider LoggerProvider { set { _logger = value.Get<ApiProxyHandler>(); } }
        #endregion

        #region Data
        private ILogger _logger;
        private readonly INonceGenerator _nonceGenerator = new OAuthNonceGenerator();
        private readonly Uri _apiUrl;

        public static ApiProxyHandler Instance { private get; set; }

        public static string ProxyPath { private get; set; }
        #endregion

        #region IHttpHandler, IRouteHandler members
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            Instance.Process(context);
        }

        internal void Process(HttpContext context)
        {
            var rawRequest = context.Request;
            HttpWebResponse response;

            var url = context.Request.RawUrl.Substring(ProxyPath.Length);

            var requestUrl = new Uri(_apiUrl, url);
            var method = !string.IsNullOrEmpty(rawRequest.Headers["X-HTTP-Method-Override"])
                    ? rawRequest.Headers["X-HTTP-Method-Override"]
                    : rawRequest.HttpMethod;

            var credentials = AuthenticationProvider.ActiveConsumer;

            //Authorized request
            if (credentials != null && credentials.Consumer != null && credentials.Token != null)
            {
                var consumerContext = new OAuthConsumerContext
                {
                    ConsumerKey = credentials.Consumer.Key,
                    ConsumerSecret = credentials.Consumer.Secret,
                    SignatureMethod = SignatureMethod.HmacSha1,
                    UseHeaderForOAuthParameters = true,
                    NonceGenerator = _nonceGenerator
                };

                var session = new OAuthSession(consumerContext, _apiUrl, _apiUrl, _apiUrl);
                var accessToken = new AccessToken
                {
                    ConsumerKey = credentials.Consumer.Key,
                    Token = credentials.Token.Key,
                    TokenSecret = credentials.Token.Secret
                };

                var body = rawRequest.BinaryRead(rawRequest.ContentLength);

                var authorizedRequest = session.Request(accessToken)
                    .ForUri(requestUrl)
                    .ForMethod(method)
                    .WithRawContent(body)
                    .WithRawContentType(rawRequest.ContentType);

                try
                {
                    authorizedRequest = authorizedRequest.SignWithToken();
                    response = authorizedRequest.ToWebResponse();
                }
                catch (WebException e)
                {
                    response = (HttpWebResponse)e.Response;

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.Debug("API call returned an unauthorized status code, disposing of active user session...");
                        AuthenticationProvider.Logout();
                    }
                }
            }
            //Non-authorized request
            else
            {
                _logger.WarnFormat("Unauthorized user tried to perform an API call: {0} {1}", method, requestUrl);

                throw new HttpException((int)HttpStatusCode.Unauthorized, "Unauthorized API call.");
            }

            using (response)
            {
                var result = context.Response;
                result.Clear();
                result.StatusCode = (int)response.StatusCode;
                result.ContentType = response.ContentType;
                result.CacheControl = response.Headers[HttpResponseHeader.CacheControl];

                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var buffer = new char[256];
                    int length;

                    while ((length = reader.Read(buffer, 0, buffer.Length)) > 0)
                        result.Write(buffer, 0, length);
                }

                result.End();

                _logger.DebugFormat("API call completed successfully: {0} {1} {2}", result.StatusCode, method, requestUrl);
            }
        }
        #endregion
    }
}
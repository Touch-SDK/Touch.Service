using System;
using System.Net.Http;
using System.ServiceModel.Web;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;

namespace Touch.ServiceModel.Authorization
{
    public static class AuthorizationServerExtensions
    {
        public static OutgoingWebResponse HandleWcfTokenRequest(this AuthorizationServer server, HttpRequestMessage request)
        {
            var context = WebOperationContext.Current;

            if (context != null)
            {
                var uri = context.IncomingRequest.UriTemplateMatch.RequestUri;
                var builder = new UriBuilder(Uri.UriSchemeHttps, uri.Host, uri.Port, uri.AbsolutePath);

                request = new HttpRequestMessage(new HttpMethod(context.IncomingRequest.Method), builder.Uri)
                {
                    Content = request.Content,
                    Version = request.Version
                };
            }

            return server.HandleTokenRequest(request);
        }
    }
}

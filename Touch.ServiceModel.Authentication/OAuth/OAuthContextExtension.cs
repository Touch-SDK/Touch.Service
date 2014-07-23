using System;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.Web;
using DevDefined.OAuth.Framework;

namespace Touch.ServiceModel.OAuth
{
    static public class OAuthContextExtension
    {
        static public IOAuthContext FromWcfMessage(this OAuthContextBuilder builder, Message message, string host = null)
        {
            var requestProperty = (HttpRequestMessageProperty)message.Properties[HttpRequestMessageProperty.Name];
            
            var uri = message.Properties.Via;
            var uriBuilder = new UriBuilder(uri);

            if (!string.IsNullOrEmpty(host))
                uriBuilder.Host = host;

            uri = uriBuilder.Uri;

            if (requestProperty.Headers.AllKeys.Contains("X-Forwarded-Proto") && uri.Scheme == "http")
            {
                var url = requestProperty.Headers["X-Forwarded-Proto"] + uri.ToString().Substring(uri.Scheme.Length);
                uri = new Uri(url);
            }

            var context = new OAuthContext
            {
                RawUri = uri,
                Headers = requestProperty.Headers,
                RequestMethod = requestProperty.Method
            };
            
            var contentType = requestProperty.Headers[HttpRequestHeader.ContentType] ?? string.Empty;
            
            if (contentType.ToLower().Contains("application/x-www-form-urlencoded"))
            {
                context.FormEncodedParameters = HttpUtility.ParseQueryString(requestProperty.QueryString);
            }

            if (requestProperty.Headers.AllKeys.Contains("Authorization"))
            {
                context.AuthorizationHeaderParameters = UriUtility.GetHeaderParameters(requestProperty.Headers["Authorization"]).ToNameValueCollection();
                context.UseAuthorizationHeader = true;
            }

            return context;
        }
    }
}

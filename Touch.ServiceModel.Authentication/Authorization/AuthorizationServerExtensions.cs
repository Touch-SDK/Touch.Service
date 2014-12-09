using System.Net.Http;
using System.Web;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;

namespace Touch.ServiceModel.Authorization
{
    public static class AuthorizationServerExtensions
    {
        public static OutgoingWebResponse HandleWcfTokenRequest(this AuthorizationServer server, HttpRequestMessage request)
        {
            var context = HttpContext.Current;

            if (context != null)
            {
                return server.HandleTokenRequest(new HttpRequestWrapper(context.Request));
            }

            return null;
        }

        public static EndUserAuthorizationRequest ReadWcfAuthorizationRequest(this AuthorizationServer server)
        {
            var context = HttpContext.Current;

            if (context != null)
            {
                return server.ReadAuthorizationRequest(new HttpRequestWrapper(context.Request));
            }

            return null;
        }
    }
}

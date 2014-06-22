using System;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Dispatcher
{
    sealed public class HttpCachePolicyMessageInspector : HttpDispatchMessageInspector
    {
        private readonly HttpCachePolicyBehavior _behavior;

        public HttpCachePolicyMessageInspector(HttpCachePolicyBehavior behavior)
        {
            _behavior = behavior;
        }

        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            // We only support caching of GET requests.
            if (!String.Equals(httpRequest.Method, "GET", StringComparison.Ordinal))
                return null;

            var cacheControl = GetCacheabilityString(_behavior.Cacheability);
            var maxAge = (int)_behavior.Duration.TotalSeconds;

            if (maxAge > 0)
            {
                if (!String.IsNullOrEmpty(cacheControl))
                {
                    cacheControl += ",";
                }

                cacheControl += "max-age=" + _behavior.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }

            return cacheControl;
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            var cacheControl = correlationState as string;

            if (!String.IsNullOrEmpty(cacheControl))
                httpResponse.Headers.Set("Cache-Control", cacheControl);
        }

        private static string GetCacheabilityString(HttpCacheability cacheability)
        {
            switch (cacheability)
            {
                case HttpCacheability.NoCache:
                    return "no-cache";
                case HttpCacheability.Private:
                    return "private";
                case HttpCacheability.Public:
                    return "public";
                default:
                    return "";
            }
        }
    }
}

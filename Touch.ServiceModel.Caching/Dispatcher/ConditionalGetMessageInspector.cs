using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using Touch.Service;

namespace Touch.ServiceModel.Dispatcher
{
    public sealed class ConditionalGetMessageInspector : HttpDispatchMessageInspector
    {
        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            if (!string.Equals(httpRequest.Method, "GET", StringComparison.Ordinal))
                return null;

            var context = WebOperationContext.Current;
            if (context == null) throw new NotSupportedException();

            var metadata = GetMetadata();

            if (metadata != null && context.IncomingRequest.IfNoneMatch != null && context.IncomingRequest.IfNoneMatch.Contains(metadata.Token))
            {
                instanceContext.Abort();
                return GetState.Unmodified;
            }

            if (metadata != null && context.IncomingRequest.IfModifiedSince != null && context.IncomingRequest.IfModifiedSince >= metadata.LastModified)
            {
                instanceContext.Abort();
                return GetState.Unmodified;
            }

            return GetState.Modified;
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            if (correlationState == null)
                return;

            if ((GetState)correlationState == GetState.Unmodified)
            {
                httpResponse.StatusCode = HttpStatusCode.NotModified;
                httpResponse.SuppressEntityBody = true;
                return;
            }

            var metadata = GetMetadata();

            if (metadata != null)
            {
                httpResponse.Headers.Add("ETag", metadata.Token);
                httpResponse.Headers.Add("Last-Modified", metadata.LastModified.ToUniversalTime().ToString("R"));
            }
        }

        private static ICacheMetadata GetMetadata()
        {
            var context = WebOperationContext.Current;
            if (context == null) return null;

            var service = OperationContext.Current.InstanceContext.GetServiceInstance() as ICacheableService;
            if (service == null || context.IncomingRequest.UriTemplateMatch == null) return null;

            var operation = (string)context.IncomingRequest.UriTemplateMatch.Data;
            var parameters = context.IncomingRequest.UriTemplateMatch.BoundVariables.AllKeys.Select(x => context.IncomingRequest.UriTemplateMatch.BoundVariables[x]).ToArray();

            var cacheKey = service.GetCacheKey(operation, parameters);
            if (cacheKey == null) return null;

            return service.GetMetadata(cacheKey);
        }

        private enum GetState { Modified, Unmodified }
    }
}

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

            if ((metadata != null && context.IncomingRequest.IfNoneMatch != null && context.IncomingRequest.IfNoneMatch.Contains(metadata.Token)) ||
                (metadata != null && context.IncomingRequest.IfModifiedSince != null && context.IncomingRequest.IfModifiedSince >= metadata.LastModified))
            {
                instanceContext.Abort();
                return new CacheState
                {
                    State = GetState.Unmodified,
                    Metadata = metadata
                };
            }

            return new CacheState
            {
                State = GetState.Modified
            };
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            var state = correlationState as CacheState;

            if (state == null)
                return;

            if (state.State == GetState.Unmodified)
            {
                httpResponse.StatusCode = HttpStatusCode.NotModified;
                httpResponse.StatusDescription = "Not Modified";
                httpResponse.SuppressEntityBody = true;
            }

            if (state.Metadata == null)
                state.Metadata = GetMetadata();

            if (state.Metadata != null)
            {
                httpResponse.Headers.Add("ETag", state.Metadata.Token);
                httpResponse.Headers.Add("Last-Modified", state.Metadata.LastModified.ToUniversalTime().ToString("R"));
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

        private class CacheState
        {
            public GetState State;
            public ICacheMetadata Metadata;
        }

        private enum GetState { Modified, Unmodified }
    }
}

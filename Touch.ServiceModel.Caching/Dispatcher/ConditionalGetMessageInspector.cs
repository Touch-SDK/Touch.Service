using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using Touch.ServiceModel.Providers;

namespace Touch.ServiceModel.Dispatcher
{
    public class ConditionalGetMessageInspector : HttpDispatchMessageInspector
    {
        public string ProviderName { get; set; }

        private string ETag
        {
            get { return ResponseMetadataProvider.GetInstance(ProviderName).ETag; }
        }

        private DateTime? LastModified
        {
            get { return ResponseMetadataProvider.GetInstance(ProviderName).LastModified; }
        }

        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            if (!String.Equals(httpRequest.Method, "GET", StringComparison.Ordinal))
                return null;

            var context = WebOperationContext.Current;
            if (context == null) throw new NotSupportedException("Missing WebOperationContext");

            if (context.IncomingRequest.IfNoneMatch != null && ETag != null && context.IncomingRequest.IfNoneMatch.Contains(ETag))
            {
                instanceContext.Abort();
                return GetState.Unmodified;
            }

            if (context.IncomingRequest.IfModifiedSince != null && LastModified != null && context.IncomingRequest.IfModifiedSince >= LastModified)
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
            
            if (ETag != null)
                httpResponse.Headers.Add("ETag", ETag);

            if (LastModified != null)
                httpResponse.Headers.Add("Last-Modified", LastModified.Value.ToUniversalTime().ToString("R"));
        }

        private enum GetState { Modified, Unmodified }
    }
}

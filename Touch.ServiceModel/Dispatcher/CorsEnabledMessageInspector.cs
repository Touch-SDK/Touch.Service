using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Dispatcher
{
    public sealed class CorsEnabledMessageInspector : HttpDispatchMessageInspector
    {
        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            var httpProp = (HttpRequestMessageProperty) request.Properties[HttpRequestMessageProperty.Name];
            object operationName;

            request.Properties.TryGetValue(WebHttpDispatchOperationSelector.HttpOperationNamePropertyName, out operationName);

            if (httpProp == null || operationName == null)
                return null;

            var origin = httpProp.Headers["Origin"];

            return origin;
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            var origin = correlationState as string;
            if (origin == null) return;

            HttpResponseMessageProperty httpProp;

            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                httpProp = (HttpResponseMessageProperty) reply.Properties[HttpResponseMessageProperty.Name];
            }
            else
            {
                httpProp = new HttpResponseMessageProperty();
                reply.Properties.Add(HttpResponseMessageProperty.Name, httpProp);
            }

            httpProp.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
}

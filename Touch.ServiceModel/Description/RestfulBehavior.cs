using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Touch.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Description
{
    /// <summary>
    /// RESTful service behavior.
    /// </summary>
    sealed public class RestfulBehavior : WebHttpBehavior
    {
        override public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            //Override HTTP method based on X-HTTP-Method-Override header
            endpointDispatcher.DispatchRuntime.OperationSelector = new HttpOverrideOperationSelector(endpointDispatcher.DispatchRuntime.OperationSelector);

            //Add support for Access-Control-Allow-Origin header
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CorsEnabledMessageInspector());
        }
    }
}

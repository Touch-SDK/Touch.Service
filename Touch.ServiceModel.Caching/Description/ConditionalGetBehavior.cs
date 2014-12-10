using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Touch.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Description
{
    sealed public class ConditionalGetBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new ConditionalGetMessageInspector());
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            var binding = endpoint.Binding;

            if (binding.Scheme != "http" && binding.Scheme != "https")
                throw new InvalidOperationException("The http condifitonal get behavior is only compatible with http and https bindings.");
        }
    }
}

using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Web;
using Touch.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Description
{
    sealed public class HttpCachePolicyBehavior : IEndpointBehavior
    {
        private HttpCacheability _cacheability = HttpCacheability.Private;

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new HttpCachePolicyMessageInspector(this));
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            var binding = endpoint.Binding;

            if (binding.Scheme != "http" && binding.Scheme != "https")
                throw new InvalidOperationException("The http cache policy behavior is only compatible with http and https bindings.");
        }

        public TimeSpan Duration { get; set; }

        public HttpCacheability Cacheability
        {
            get
            {
                return _cacheability;
            }
            set
            {
                _cacheability = value;
            }
        }
    }
}

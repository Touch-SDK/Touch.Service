using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Dispatcher
{
    /// <summary>
    /// Overrides HTTP method based on X-HTTP-Method-Override header.
    /// </summary>
    sealed class HttpOverrideOperationSelector : IDispatchOperationSelector
    {
        public const string HttpMethodOverrideHeaderName = "X-HTTP-Method-Override";
        public const string OriginalHttpMethodPropertyName = "OriginalHttpMethod";

        private readonly IDispatchOperationSelector _originalSelector;

        public HttpOverrideOperationSelector(IDispatchOperationSelector originalSelector)
        {
            _originalSelector = originalSelector;
        }

        public string SelectOperation(ref Message message)
        {
            if (message.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                var reqProp = (HttpRequestMessageProperty)message.Properties[HttpRequestMessageProperty.Name];
                var httpMethodOverride = reqProp.Headers[HttpMethodOverrideHeaderName];

                if (!String.IsNullOrEmpty(httpMethodOverride))
                {
                    message.Properties[OriginalHttpMethodPropertyName] = reqProp.Method;
                    reqProp.Method = httpMethodOverride;
                }
            }

            return _originalSelector.SelectOperation(ref message);
        }
    }
}

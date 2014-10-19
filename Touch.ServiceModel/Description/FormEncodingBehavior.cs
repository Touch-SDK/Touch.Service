using System;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using Touch.ServiceModel.Formatters;

namespace Touch.ServiceModel.Description
{
    sealed public class FormEncodingBehavior : WebHttpBehavior
    {
        public string ResponseContentType { get; set; }

        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            var converter = GetQueryStringConverter(operationDescription);

            return new HtmlFormResponseDispatchFormatter(operationDescription, converter)
            {
                ResponseContentType = ResponseContentType
            };
        }

        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            // This is the default formatter for the operation, which we may wrap with our custom formatter
            IDispatchMessageFormatter formatter = base.GetRequestDispatchFormatter(operationDescription, endpoint);

            // Messages[0] is the request message
            int partsCount = operationDescription.Messages[0].Body.Parts.Count;

            // If the message doesn't have any parts, then it can't have any body content so a form post isn't applicable
            if (partsCount == 0)
            {
                return formatter;
            }

            //For [WebInvoke] operations with body content, we want to wrap the base formatter with our HtmlFormRequestDispatchFormatter
            var webInvoke = operationDescription.Behaviors.Find<WebInvokeAttribute>();

            if (webInvoke != null)
            {
                if (webInvoke.BodyStyle == WebMessageBodyStyle.Wrapped)
                {
                    throw new InvalidOperationException("The FormProcessingBehavior does not support wrapped requests or responses.");
                }

                // We need to determine the parts of the message that are associated with the Uri and those that are associated
                //  with the body content of the request.  To do this, we need to get the Uri template for the operation and determine
                //  how many parameters it has
                UriTemplate uriTemplate;
                var bodyPartsCount = partsCount;

                if (!string.IsNullOrEmpty(webInvoke.UriTemplate))
                {
                    uriTemplate = new UriTemplate(webInvoke.UriTemplate);
                    // The number of message parts for the request body will be the equal to the total parts for the message minus
                    //  the total number of UriTemplate variables
                    bodyPartsCount = partsCount - (uriTemplate.PathSegmentVariableNames.Count + uriTemplate.QueryValueVariableNames.Count);
                }
                else
                {
                    uriTemplate = new UriTemplate(string.Empty);
                }

                // Since we've disallowed wrapped message bodies, we can be sure that the message will only have
                //  0 or 1 message parts that are associated with the body of the request.
                if (bodyPartsCount == 1)
                {
                    var converter = GetQueryStringConverter(operationDescription);
                    formatter = new HtmlFormRequestDispatchFormatter(operationDescription, uriTemplate, converter, formatter);
                }
            }

            return formatter;
        }
    }
}

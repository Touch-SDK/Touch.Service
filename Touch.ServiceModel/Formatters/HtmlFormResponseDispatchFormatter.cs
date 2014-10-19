using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Web;

namespace Touch.ServiceModel.Formatters
{
    sealed class HtmlFormResponseDispatchFormatter : IDispatchMessageFormatter
    {
        public string ResponseContentType { get; set; }

        private readonly QueryStringConverter _converter;

        private Type BodyParameterType { get; set; }
        private string OperationName { get; set; }

        public HtmlFormResponseDispatchFormatter(OperationDescription operation, QueryStringConverter converter)
        {
            _converter = converter;

            try
            {
                BodyParameterType = operation.Messages[1].Body.ReturnValue.Type;
                OperationName = operation.Messages[1].Body.ReturnValue.Name;
            }
            catch (Exception e)
            {
                throw new SerializationException("Unable to serialize response.", e);
            }

            var canConvertBodyType = _converter.CanConvert(BodyParameterType) || BodyParameterType == typeof(HttpResponseMessage);

            if (!canConvertBodyType && BodyParameterType.GetCustomAttributes(typeof(DataContractAttribute), false).Length == 0)
            {
                if (BodyParameterType == typeof(void)) return;

                throw new NotSupportedException(
                    string.Format("Body parameter '{0}' from operation '{1}' is of type '{2}', which is not decorated with a DataContractAttribute. " +
                                  "Only body parameter types decorated with the DataContractAttribute are supported.",
                    OperationName,
                    operation.Name,
                    BodyParameterType));
            }
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            throw new NotSupportedException("The BodyResponseFormatter only supports deserializing outcoming responses.");
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            if(WebOperationContext.Current == null)
                throw new SerializationException("Web operation context is not provided.");

            string body;

            if (BodyParameterType == typeof (void))
                return WebOperationContext.Current.CreateTextResponse(string.Empty, ResponseContentType);

            if (_converter.CanConvert(BodyParameterType))
            {
                body = _converter.ConvertValueToString(result, BodyParameterType);
            }
            else if(BodyParameterType == typeof(HttpResponseMessage))
            {
                var data = (HttpResponseMessage)result;

                body = data.Content.ReadAsStringAsync().Result;
            }
            else
            {
                var bodyMembers = new NameValueCollection();

                foreach (var propertyInfo in BodyParameterType.GetProperties())
                {
                    if (propertyInfo.CanWrite)
                    {
                        object[] dataMemberAttributes = propertyInfo.GetCustomAttributes(typeof(DataMemberAttribute), false);
                        if (dataMemberAttributes.Length > 0 && propertyInfo.CanWrite)
                        {
                            if (!_converter.CanConvert(propertyInfo.PropertyType))
                            {
                                throw new NotSupportedException(
                                    string.Format("Body parameter from operation '{0}' is of type '{1}', which has the property '{2}' of type '{3}' that is decorated with a DataMemberAttribute but which can not be converted with the given query string converter.  " +
                                                  "Only body parameters types in which all properties decorated with the DataMemberAttribute can be converted using the given query string converter are supported.",
                                    OperationName,
                                    BodyParameterType,
                                    propertyInfo.Name,
                                    propertyInfo.PropertyType));
                            }

                            var dataMemberAttribute = (DataMemberAttribute)dataMemberAttributes[0];
                            var dataMemberName = (string.IsNullOrEmpty(dataMemberAttribute.Name)) ? propertyInfo.Name : dataMemberAttribute.Name;

                            var value = propertyInfo.GetValue(result, null);
                            bodyMembers[dataMemberName] = _converter.ConvertValueToString(value, propertyInfo.PropertyType);
                        }
                    }
                }

                body = string.Join("&", Array.ConvertAll(bodyMembers.AllKeys, key => string.Format("{0}={1}", HttpUtility.UrlEncode(key, Encoding.ASCII), HttpUtility.UrlEncode(bodyMembers[key], Encoding.ASCII))));
            }

            return WebOperationContext.Current.CreateTextResponse(
                body ?? string.Empty, 
                ResponseContentType,
                Encoding.ASCII
            );
        }
    }
}

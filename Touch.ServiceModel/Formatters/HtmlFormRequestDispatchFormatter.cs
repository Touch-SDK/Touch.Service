using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;

namespace Touch.ServiceModel.Formatters
{
    sealed class HtmlFormRequestDispatchFormatter : RequestBodyDispatchFormatter
    {
        const BindingFlags PublicInstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public;
        readonly bool _canConvertBodyType;
        readonly Dictionary<string, BodyMemberData> _bodyMembers;
        readonly string[] _requiredBodyMembers;

        public HtmlFormRequestDispatchFormatter(OperationDescription operation, UriTemplate uriTemplate, QueryStringConverter converter, IDispatchMessageFormatter innerFormatter) :
            base(operation, uriTemplate, converter, innerFormatter)
        {
            // This formatter will only support deserializing form post data to a type if:
            //  (1) The type can be converted via the QueryStringConverter or...
            //  (2) The type meets the following requirements:
            //      (A) The type is decorated with the DataContractAttribute
            //      (B) Every public field or property that is decorated with the DataMemberAttribute is of a type that
            //          can be converted by the QueryStringConverter

            _canConvertBodyType = QueryStringConverter.CanConvert(BodyParameterType);

            if (!_canConvertBodyType)
            {
                if (BodyParameterType.GetCustomAttributes(typeof(DataContractAttribute), false).Length == 0)
                {
                    throw new NotSupportedException(
                        string.Format("Body parameter '{0}' from operation '{1}' is of type '{2}', which is not decorated with a DataContractAttribute.  " +
                                      "Only body parameter types decorated with the DataContractAttribute are supported.",
                        BodyParameterName,
                        operation.Name,
                        BodyParameterType));
                }

                // For the body type, we'll need to cache information about each of the public fields/properties 
                //  that is decorated with the DataMemberAttribute; we'll store this info in the bodyMembers dictionary
                //  where the member name is the dictionary key
                _bodyMembers = new Dictionary<string, BodyMemberData>();

                GetBobyMemberDataForFields(operation.Name);
                GetBodyMemberDataForProperties(operation.Name);

                _requiredBodyMembers = _bodyMembers.Where(p => p.Value.IsRequired).Select(p => p.Key).ToArray();
            }
        }

        protected override object DeserializeRequestBody(Stream body)
        {
            NameValueCollection parsedForm = ParseBodyAsNameValueCollection(body);

            // If we can covert the message body type via the QueryStringConverter then the 
            //  NameValueCollection should have a named value for the message body type
            if (_canConvertBodyType)
            {
                return QueryStringConverter.ConvertValueToString(parsedForm[BodyParameterName], BodyParameterType);
            }

            // If we reached here, then we have a message body type that can't just be converted with the 
            //  QueryStringConverter.  So we'll have to create an instance of the message body type and then
            //  set the property and field values on the instance with the values from the NameValueCollection
            var typedBody = Activator.CreateInstance(BodyParameterType);

            // We also need to track that the required members were supplied in the form post data
            List<string> copyOfRequiredBodyMembers = null;

            if (_requiredBodyMembers != null)
            {
                copyOfRequiredBodyMembers = _requiredBodyMembers.ToList();
            }

            foreach (var formDataName in parsedForm.AllKeys)
            {
                var memberName = formDataName;

                // Check if the member name is prepended with the body parameter name
                if (memberName.StartsWith(BodyParameterName + ".", StringComparison.Ordinal))
                {
                    memberName = memberName.Substring(BodyParameterName.Length + 1);
                }

                BodyMemberData memberData;

                if (_bodyMembers != null && _bodyMembers.TryGetValue(memberName, out memberData) && memberData != null)
                {
                    var value = QueryStringConverter.ConvertStringToValue(parsedForm[formDataName], memberData.Type);

                    if (memberData.IsProperty)
                        memberData.PropertyInfo.SetValue(typedBody, value, null);
                    else
                        memberData.FieldInfo.SetValue(typedBody, value);

                    if (memberData.IsRequired && copyOfRequiredBodyMembers != null)
                        copyOfRequiredBodyMembers.Remove(memberName);
                }
            }

            EnsureAllRequiredFieldsWerePresent(copyOfRequiredBodyMembers);

            return typedBody;
        }


        protected override bool CanDeserializeRequestBody(Message message)
        {
            // We only want to deserialize requests with the given content type; for all other requests
            //  we'll let the default WCF formatter do the deserialization
            return WebOperationContext.Current != null && WebOperationContext.Current.IncomingRequest.ContentType == "application/x-www-form-urlencoded";
        }

        private static NameValueCollection ParseBodyAsNameValueCollection(Stream body)
        {
            string formData;

            using (var reader = new StreamReader(body))
            {
                formData = reader.ReadToEnd();
            }

            var parsedForm = System.Web.HttpUtility.ParseQueryString(formData);
            return parsedForm;
        }

        private static void EnsureAllRequiredFieldsWerePresent(IList<string> copyOfRequiredBodyMembers)
        {
            if (copyOfRequiredBodyMembers.Count > 0)
            {
                var stringBuilder = new StringBuilder("The form data set did not contain the following required control names: ");

                foreach (var requiredBodyMember in copyOfRequiredBodyMembers)
                    stringBuilder.Append(requiredBodyMember + ",");

                stringBuilder.Replace(",", ".", stringBuilder.Length, 1);

                throw new WebFaultException<string>(stringBuilder.ToString(), HttpStatusCode.BadRequest);
            }
        }

        private void GetBodyMemberDataForProperties(string operationName)
        {
            foreach (var propertyInfo in BodyParameterType.GetProperties(PublicInstanceBindingFlags))
            {
                if (propertyInfo.CanWrite)
                {
                    var dataMemberAttributes = propertyInfo.GetCustomAttributes(typeof(DataMemberAttribute), false);

                    if (dataMemberAttributes.Length > 0 && propertyInfo.CanWrite)
                    {
                        if (!QueryStringConverter.CanConvert(propertyInfo.PropertyType))
                        {
                            throw new NotSupportedException(
                                string.Format("Body parameter '{0}' from operation '{1}' is of type '{2}', which has the property '{3}' of type '{4}' that is decorated with a DataMemberAttribute but which can not be converted with the given query string converter. " +
                                              "Only body parameters types in which all properties decorated with the DataMemberAttribute can be converted using the given query string converter are supported.",
                                BodyParameterName,
                                operationName,
                                BodyParameterType,
                                propertyInfo.Name,
                                propertyInfo.PropertyType));
                        }

                        var dataMemberAttribute = (DataMemberAttribute)dataMemberAttributes[0];
                        var dataMemberName = (string.IsNullOrEmpty(dataMemberAttribute.Name)) 
                            ? propertyInfo.Name 
                            : dataMemberAttribute.Name;

                        _bodyMembers.Add(dataMemberName, new BodyMemberData(propertyInfo, dataMemberAttribute.IsRequired));
                    }
                }
            }
        }

        private void GetBobyMemberDataForFields(string operationName)
        {
            foreach (var fieldInfo in BodyParameterType.GetFields(PublicInstanceBindingFlags))
            {
                var dataMemberAttributes = fieldInfo.GetCustomAttributes(typeof(DataMemberAttribute), false);

                if (dataMemberAttributes.Length > 0)
                {
                    if (!QueryStringConverter.CanConvert(fieldInfo.FieldType))
                    {
                        throw new NotSupportedException(
                            string.Format("Body parameter '{0}' from operation '{1}' is of type '{2}', which has the field '{3}' of type '{4}' that is decorated with a DataMemberAttribute but which can not be converted with the given query string converter.  " +
                                          "Only body parameters types in which all fields decorated with the DataMemberAttribute can be converted using the given query string converter are supported.",
                            BodyParameterName,
                            operationName,
                            BodyParameterType,
                            fieldInfo.Name,
                            fieldInfo.FieldType));
                    }

                    var dataMemberAttribute = dataMemberAttributes[0] as DataMemberAttribute;

                    if (dataMemberAttribute != null)
                    {
                        var dataMemberName = (string.IsNullOrEmpty(dataMemberAttribute.Name)) ? fieldInfo.Name : dataMemberAttribute.Name;
                        _bodyMembers.Add(dataMemberName, new BodyMemberData(fieldInfo, dataMemberAttribute.IsRequired));
                    }
                }
            }
        }

        class BodyMemberData
        {
            public BodyMemberData(PropertyInfo propertyInfo, bool isRequired)
            {
                if (propertyInfo == null)
                {
                    throw new ArgumentNullException("propertyInfo");
                }
                IsRequired = isRequired;
                Type = propertyInfo.PropertyType;
                IsProperty = true;
                PropertyInfo = propertyInfo;
            }

            public BodyMemberData(FieldInfo fieldInfo, bool isRequired)
            {
                if (fieldInfo == null)
                {
                    throw new ArgumentNullException("fieldInfo");
                }
                IsRequired = isRequired;
                Type = fieldInfo.FieldType;
                IsProperty = false;
                FieldInfo = fieldInfo;
            }

            public Type Type
            {
                get;
                private set;
            }

            public bool IsRequired
            {
                get;
                private set;
            }

            public bool IsProperty
            {
                get;
                private set;
            }

            public FieldInfo FieldInfo
            {
                get;
                private set;
            }

            public PropertyInfo PropertyInfo
            {
                get;
                private set;
            }
        }
    }
}

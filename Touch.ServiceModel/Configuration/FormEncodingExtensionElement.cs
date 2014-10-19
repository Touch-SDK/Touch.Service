using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class FormEncodingExtensionElement : BehaviorExtensionElement
    {
        public const string DefaultResponseContentType = "application/x-www-form-urlencoded";

        public FormEncodingExtensionElement()
        {
            ResponseContentType = DefaultResponseContentType;
        }

        #region Configuration
        /// <summary>
        /// Response Content-Type.
        /// </summary>
        [ConfigurationProperty("responseContentType", DefaultValue = DefaultResponseContentType)]
        public string ResponseContentType
        {
            get { return (string)this["responseContentType"]; }
            set { this["responseContentType"] = value; }
        }
        #endregion

        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        public override Type BehaviorType { get { return typeof(FormEncodingBehavior); } }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        protected override object CreateBehavior()
        {
            return new FormEncodingBehavior
            {
                ResponseContentType = ResponseContentType
            };
        }
    }
}

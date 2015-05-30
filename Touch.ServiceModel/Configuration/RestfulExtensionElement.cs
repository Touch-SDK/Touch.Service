using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class RestfulExtensionElement : BehaviorExtensionElement
    {
        #region Configuration
        /// <summary>
        /// Enable CORS.
        /// </summary>
        [ConfigurationProperty("enableCors", DefaultValue = false)]
        public bool EnableCors
        {
            get { return Convert.ToBoolean(this["enableCors"]); }
            set { this["enableCors"] = value; }
        }

        /// <summary>
        /// Enable JSON errors.
        /// </summary>
        [ConfigurationProperty("jsonErrors", DefaultValue = false)]
        public bool JsonErrors
        {
            get { return Convert.ToBoolean(this["jsonErrors"]); }
            set { this["jsonErrors"] = value; }
        }

        /// <summary>
        /// Show exception details.
        /// </summary>
        [ConfigurationProperty("showExceptionDetails", DefaultValue = false)]
        public bool ShowExceptionDetails
        {
            get { return Convert.ToBoolean(this["showExceptionDetails"]); }
            set { this["showExceptionDetails"] = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        public override Type BehaviorType { get { return typeof(RestfulBehavior); } }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        protected override object CreateBehavior()
        {
            return new RestfulBehavior
            {
                EnableCors = EnableCors,
                JsonErrors = JsonErrors,
                ShowExceptionDetails = ShowExceptionDetails
            };
        }
        #endregion
    }
}

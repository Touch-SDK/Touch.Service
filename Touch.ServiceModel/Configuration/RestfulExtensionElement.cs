using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class RestfulExtensionElement : BehaviorExtensionElement
    {
        #region Configuration

        #endregion

        #region Methods
        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        public override Type BehaviorType { get { return typeof(RestfulBehavior); } }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        protected override object CreateBehavior() { return new RestfulBehavior(); } 
        #endregion
    }
}

using System;
using System.ServiceModel.Configuration;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class FormEncodingExtensionElement : BehaviorExtensionElement
    {
        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        public override Type BehaviorType { get { return typeof(FormEncodingBehavior); } }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        protected override object CreateBehavior() { return new FormEncodingBehavior(); }
    }
}

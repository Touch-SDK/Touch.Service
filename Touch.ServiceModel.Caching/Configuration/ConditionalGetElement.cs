using System;
using System.ServiceModel.Configuration;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class ConditionalGetElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(ConditionalGetBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new ConditionalGetBehavior();
        }
    }
}

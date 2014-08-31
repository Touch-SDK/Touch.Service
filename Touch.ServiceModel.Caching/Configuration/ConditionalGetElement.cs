using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Web;
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
            return new ConditionalGetBehavior { ProviderName = ProviderName };
        }

        [ConfigurationProperty("providerName", IsRequired = true)]
        public string ProviderName
        {
            get
            {
                return this["providerName"] as string;
            }
            set
            {
                this["providerName"] = value;
            }
        }
    }
}

using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Web;
using Touch.ServiceModel.Description;

namespace Touch.ServiceModel.Configuration
{
    sealed public class HttpCachePolicyElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(HttpCachePolicyBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new HttpCachePolicyBehavior { Duration = Duration, Cacheability = Cacheability };
        }

        [ConfigurationProperty("duration", IsRequired = false)]
        public TimeSpan Duration
        {
            get
            {
                return (this["duration"] as TimeSpan?) ?? TimeSpan.Zero;
            }
            set
            {
                this["duration"] = value;
            }
        }

        [ConfigurationProperty("cacheability", IsRequired = false, DefaultValue = HttpCacheability.Private)]
        public HttpCacheability Cacheability
        {
            get
            {
                return (this["cacheability"] as HttpCacheability?) ?? HttpCacheability.Private;
            }
            set
            {
                this["cacheability"] = value;
            }
        }
    }
}

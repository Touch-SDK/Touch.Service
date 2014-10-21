using System.ServiceModel;
using Touch.Domain;

namespace Touch.Providers
{
    /// <summary>
    /// WCF authentication provider.
    /// </summary>
    sealed public class WcfAuthenticationProvider : IAuthenticationProvider
    {
        #region IAuthenticationProvider implementation
        public Credentials ActiveConsumer
        {
            get
            {
                var context = OperationContext.Current;
                if (context == null) return null;

                if (!context.IncomingMessageProperties.ContainsKey("Credentials"))
                    return null;

                return (Credentials)context.IncomingMessageProperties["Credentials"];
            }
        }

        public OAuth2User ActiveUser
        {
            get
            {
                var context = OperationContext.Current;
                if (context == null) return null;

                if (!context.IncomingMessageProperties.ContainsKey("OAuth2User"))
                    return null;

                return (OAuth2User)context.IncomingMessageProperties["OAuth2User"];
            }
        }
        #endregion
    }
}

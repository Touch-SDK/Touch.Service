using System;
using System.ServiceModel;
using Touch.Domain;

namespace Touch.Providers
{
    /// <summary>
    /// WCF OAuth2 authentication provider.
    /// </summary>
    sealed public class OAuth2AuthenticationProvider : IOAuth2Provider
    {
        #region IOAuth2Provider implementation
        public OAuth2Access ActiveAccess
        {
            get
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                if (!context.IncomingMessageProperties.ContainsKey("OAuth2Access"))
                    return null;

                return (OAuth2Access)context.IncomingMessageProperties["OAuth2Access"];
            }
            set
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                context.IncomingMessageProperties["OAuth2Access"] = value;
            }
        }
        #endregion
    }
}

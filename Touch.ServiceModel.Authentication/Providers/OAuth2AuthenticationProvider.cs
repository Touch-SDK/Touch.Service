using System;
using System.ServiceModel;
using Touch.Domain;

namespace Touch.Providers
{
    /// <summary>
    /// WCF OAuth2 authentication provider.
    /// </summary>
    sealed public class WcfOAuthProvider : IOAuth2Provider
    {
        #region IOAuth2Provider implementation
        OAuth2User IOAuth2Provider.CurrentUser
        {
            get
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                if (!context.IncomingMessageProperties.ContainsKey("OAuth2User"))
                    return null;

                return (OAuth2User)context.IncomingMessageProperties["OAuth2User"];
            }
            set
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                context.IncomingMessageProperties["OAuth2User"] = value;
            }
        }
        #endregion
    }
}

using System;
using System.ServiceModel;
using Touch.Domain;

namespace Touch.Providers
{
    /// <summary>
    /// WCF OAuth authentication provider.
    /// </summary>
    sealed public class OAuthAuthenticationProvider : IOAuthProvider
    {
        #region IOAuthProvider implementation
        OAuthCredentials IOAuthProvider.CurrentUser
        {
            get
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                if (!context.IncomingMessageProperties.ContainsKey("OAuthUser"))
                    return null;

                return (OAuthCredentials)context.IncomingMessageProperties["OAuthUser"];
            }
            set
            {
                var context = OperationContext.Current;
                if (context == null) throw new OperationCanceledException();

                context.IncomingMessageProperties["OAuthUser"] = value;
            }
        }
        #endregion
    }
}

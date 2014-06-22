using System;
using System.ServiceModel;
using Touch.Domain;
using Touch.Persistence;

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

        public void Authenticate(string username, Credentials credentials)
        {
            throw new NotSupportedException();
        }

        public void Logout()
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}

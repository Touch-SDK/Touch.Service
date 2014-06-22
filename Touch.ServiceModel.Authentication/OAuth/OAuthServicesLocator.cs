using System;
using DevDefined.OAuth.Provider;

namespace Touch.ServiceModel.OAuth
{
    sealed public class OAuthServicesLocator
    {
        #region Instance
        public OAuthServicesLocator(IOAuthProvider oAuthProvider, IOAuthProvider xAuthProvider, OAuthTokenStore tokenStore)
        {
            OAuthProvider = oAuthProvider;
            XAuthProvider = xAuthProvider;
            TokenStore = tokenStore;
        }

        public IOAuthProvider OAuthProvider { get; private set; }

        public IOAuthProvider XAuthProvider { get; private set; }

        public OAuthTokenStore TokenStore { get; private set; }
        #endregion

        #region Static
        private static OAuthServicesLocator _instance;
        private static Func<OAuthServicesLocator> _resolver;

        public static Func<OAuthServicesLocator> Resolver { set { _resolver = value; } }

        public static OAuthServicesLocator Current { get { return _instance ?? (_instance = _resolver.Invoke()); } }
        #endregion
    }
}
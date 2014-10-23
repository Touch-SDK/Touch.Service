using System;
using DevDefined.OAuth;
using DevDefined.OAuth.Framework;
using Touch.Logic;

namespace Touch.ServiceModel.OAuth
{
    sealed public class OAuthNonceGenerator : INonceGenerator
    {
        #region Dependencies
        public OAuthLogic AuthenticationLogic { private get; set; }
        #endregion

        public string GenerateNonce(IOAuthContext context)
        {
            return AuthenticationLogic.GenerateApiNonce();
        }
    }
}

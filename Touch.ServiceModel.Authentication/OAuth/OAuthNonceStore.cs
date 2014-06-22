using System;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage;
using Touch.Logic;

namespace Touch.ServiceModel.OAuth
{
    sealed public class OAuthNonceStore : INonceStore
    {
        #region Dependencies
        public AuthenticationLogic AuthenticationLogic { private get; set; }
        #endregion

        public bool RecordNonceAndCheckIsUnique(IConsumer consumer, string nonce)
        {
            return AuthenticationLogic.ConsumeNonce(nonce);
        }
    }
}

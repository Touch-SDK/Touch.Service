using System;
using System.Collections.Generic;
using System.Linq;
using DotNetOpenAuth.Messaging.Bindings;
using Touch.Domain;
using Touch.Logic;

namespace Touch.ServiceModel.OAuth
{
    public sealed class OAuth2KeyStore : INonceStore, ICryptoKeyStore
    {
        #region Dependencies
        public IOAuth2KeyLogic KeyLogic { private get; set; }
        public IOAuth2NonceLogic NonceLogic { private get; set; }
        #endregion

        #region INonceStore Members
        public bool StoreNonce(string context, string nonce, DateTime timestampUtc)
        {
            var entry = new OAuth2Nonce
            {
                HashKey = nonce,
                Context = context,
                IssueDate = timestampUtc.ToDocumentString()
            };

            return NonceLogic.StoreNonce(entry);
        }
        #endregion

        #region ICryptoKeyStore Members
        public CryptoKey GetKey(string bucket, string handle)
        {
            var key = KeyLogic.GetKey(bucket, handle);

            return key != null
                ? new CryptoKey(Convert.FromBase64String(key.Secret), key.ExpirationDate.FromDocumentString())
                : null;
        }

        public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket)
        {
            var result = (from key in KeyLogic.GetKeys(bucket)
                          let expires = key.ExpirationDate.FromDocumentString()
                          orderby expires descending
                          select new KeyValuePair<string, CryptoKey>(key.Handle, new CryptoKey(Convert.FromBase64String(key.Secret), expires))).ToArray();
            return result;
        }

        public void StoreKey(string bucket, string handle, CryptoKey key)
        {
            var entry = new OAuth2Key
            {
                Handle = handle,
                Secret = Convert.ToBase64String(key.Key),
                Bucket = bucket,
                ExpirationDate = key.ExpiresUtc.ToDocumentString()
            };

            KeyLogic.StoreKey(entry);
        }

        public void RemoveKey(string bucket, string handle)
        {
            KeyLogic.RemoveKey(bucket, handle);
        }
        #endregion
    }
}

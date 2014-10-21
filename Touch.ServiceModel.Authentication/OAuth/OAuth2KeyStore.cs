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
        public OAuth2Logic Logic { private get; set; }
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

            try
            {
                Logic.StoreNonce(entry);
                return true; //if the context+nonce+timestamp (combination) was not previously in the database.
            }
            catch
            {
                return false; //if the nonce was stored previously with the same timestamp and context.
            }
        }
        #endregion

        #region ICryptoKeyStore Members
        public CryptoKey GetKey(string bucket, string handle)
        {
            var key = Logic.GetKey(bucket, handle);

            return key != null
                ? new CryptoKey(Convert.FromBase64String(key.Secret), key.ExpirationDate.FromDocumentString())
                : null;
        }

        public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket)
        {
            var result = (from key in Logic.GetKeys(bucket)
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

            Logic.StoreKey(entry);
        }

        public void RemoveKey(string bucket, string handle)
        {
            Logic.RemoveKey(bucket, handle);
        }
        #endregion
    }
}

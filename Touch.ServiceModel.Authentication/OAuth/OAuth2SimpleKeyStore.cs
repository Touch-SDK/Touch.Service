using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotNetOpenAuth.Messaging.Bindings;

namespace Touch.ServiceModel.OAuth
{
    public sealed class OAuth2SimpleKeyStore : INonceStore, ICryptoKeyStore
    {
        class CryptoKeyStoreEntry
        {
            public string Bucket { get; set; }
            public string Handle { get; set; }
            public CryptoKey Key { get; set; }
        }

        private readonly List<CryptoKeyStoreEntry> _keys = new List<CryptoKeyStoreEntry>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public CryptoKey GetKey(string bucket, string handle)
        {
            return _keys.Where(k => k.Bucket == bucket && k.Handle == handle)
                                    .Select(k => k.Key)
                                    .FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket)
        {
            return _keys.Where(k => k.Bucket == bucket)
                                   .OrderByDescending(k => k.Key.ExpiresUtc)
                                   .Select(k => new KeyValuePair<string, CryptoKey>(k.Handle, k.Key));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RemoveKey(string bucket, string handle)
        {
            _keys.RemoveAll(k => k.Bucket == bucket && k.Handle == handle);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StoreKey(string bucket, string handle, CryptoKey key)
        {
            var entry = new CryptoKeyStoreEntry();
            entry.Bucket = bucket;
            entry.Handle = handle;
            entry.Key = key;
            _keys.Add(entry);
        }

        public bool StoreNonce(string context, string nonce, DateTime timestampUtc)
        {
            return true;
        }
    }
}

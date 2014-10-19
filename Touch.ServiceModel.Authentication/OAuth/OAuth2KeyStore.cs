using System;
using System.Collections.Generic;
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
		/// <summary>
		/// Stores a given nonce and timestamp.
		/// </summary>
		/// <param name="context">The context, or namespace, within which the
		/// <paramref name="nonce"/> must be unique.
		/// The context SHOULD be treated as case-sensitive.
		/// The value will never be <c>null</c> but may be the empty string.</param>
		/// <param name="nonce">A series of random characters.</param>
		/// <param name="timestampUtc">The UTC timestamp that together with the nonce string make it unique
		/// within the given <paramref name="context"/>.
		/// The timestamp may also be used by the data store to clear out old nonces.</param>
		/// <returns>
		/// True if the context+nonce+timestamp (combination) was not previously in the database.
		/// False if the nonce was stored previously with the same timestamp and context.
		/// </returns>
		/// <remarks>
		/// The nonce must be stored for no less than the maximum time window a message may
		/// be processed within before being discarded as an expired message.
		/// This maximum message age can be looked up via the
		/// <see cref="DotNetOpenAuth.Configuration.MessagingElement.MaximumMessageLifetime"/>
		/// property, accessible via the DotNetOpenAuth.Configuration.DotNetOpenAuthSection.Configuration
		/// property.
		/// </remarks>
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
		        return true;
		    }
		    catch
		    {
                return false;
		    }
		}
		#endregion

		#region ICryptoKeyStore Members
		public CryptoKey GetKey(string bucket, string handle)
		{
		    return null;
		    // It is critical that this lookup be case-sensitive, which can only be configured at the database.
		    /*var matches = from key in MvcApplication.DataContext.SymmetricCryptoKeys
						  where key.Bucket == bucket && key.Handle == handle
						  select new CryptoKey(key.Secret, key.ExpiresUtc.AsUtc());

			return matches.FirstOrDefault();*/
		}

		public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket)
        {
            return null;
			/*return from key in MvcApplication.DataContext.SymmetricCryptoKeys
				   where key.Bucket == bucket
				   orderby key.ExpiresUtc descending
				   select new KeyValuePair<string, CryptoKey>(key.Handle, new CryptoKey(key.Secret, key.ExpiresUtc.AsUtc()));*/
            
		}

		public void StoreKey(string bucket, string handle, CryptoKey key)
		{
		    return;
		    /*var keyRow = new SymmetricCryptoKey() {
				Bucket = bucket,
				Handle = handle,
				Secret = key.Key,
				ExpiresUtc = key.ExpiresUtc,
			};

			MvcApplication.DataContext.SymmetricCryptoKeys.InsertOnSubmit(keyRow);
			MvcApplication.DataContext.SubmitChanges();*/
		}

		public void RemoveKey(string bucket, string handle)
		{
		    return;
		    /*var match = MvcApplication.DataContext.SymmetricCryptoKeys.FirstOrDefault(k => k.Bucket == bucket && k.Handle == handle);
			if (match != null) {
				MvcApplication.DataContext.SymmetricCryptoKeys.DeleteOnSubmit(match);
			}*/
		}
		#endregion
    }
}

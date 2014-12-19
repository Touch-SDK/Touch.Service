using System;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;

namespace Touch.ServiceModel.OAuth
{
    internal sealed class OAuth2ClientDescription : Domain.OAuth2Client, IClientDescription
    {
        #region .ctor
        public OAuth2ClientDescription()
        { }

        public OAuth2ClientDescription(Domain.OAuth2Client data)
        {
            HashKey = data.HashKey;
            Secret = data.Secret;
            Callback = data.Callback;
            IsPublic = data.IsPublic;
        } 
        #endregion

        #region IClientDescription members
        public ClientType ClientType
        {
            get { return IsPublic ? ClientType.Public : ClientType.Confidential; }
        }

        public Uri DefaultCallback
        {
            get { return string.IsNullOrEmpty(Callback) ? null : new Uri(Callback); }
        }

        public bool HasNonEmptySecret
        {
            get { return !string.IsNullOrEmpty(Secret); }
        }

        public bool IsCallbackAllowed(Uri callback)
        {
            if (string.IsNullOrEmpty(Callback))
                return true;

            var acceptableCallbackPattern = new Uri(Callback);

            return string.Equals(acceptableCallbackPattern.GetLeftPart(UriPartial.Authority), callback.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal);
        }

        public bool IsValidClientSecret(string secret)
        {
            return MessagingUtilities.EqualsConstantTime(secret, Secret);
        } 
        #endregion
    }
}

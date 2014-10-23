using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage;
using Touch.Logic;

namespace Touch.ServiceModel.OAuth
{
    sealed public class OAuthConsumerStore : IConsumerStore
    {
        #region Dependencies
        public OAuthLogic AuthenticationLogic { private get; set; }
        #endregion

        #region IConsumerStore implementation
        public bool IsConsumer(IConsumer consumer)
        {
            return !string.IsNullOrEmpty(consumer.ConsumerKey);
        }

        public string GetConsumerSecret(IOAuthContext context)
        {
            var consumer = AuthenticationLogic.GetConsumer(context.ConsumerKey);

            if (consumer == null)
                throw new OAuthException(context, OAuthProblems.ConsumerKeyRejected, "Consumer not found.");

            return consumer.Secret;
        }

        public void SetConsumerSecret(IConsumer consumer, string consumerSecret)
        {
            throw new NotSupportedException();
        }

        public void SetConsumerCertificate(IConsumer consumer, X509Certificate2 certificate)
        {
            throw new NotSupportedException();
        }

        public AsymmetricAlgorithm GetConsumerPublicKey(IConsumer consumer)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}

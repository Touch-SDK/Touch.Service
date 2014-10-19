using System.Security.Cryptography;

namespace Touch.ServiceModel.OAuth
{
    public interface IOAuth2CryptoService
    {
        RSACryptoServiceProvider GetSigningProvider();

        RSACryptoServiceProvider GetEncryptionProvider();
    }
}

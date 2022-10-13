using System.Threading.Tasks;

namespace NTMC.CryptoGraphy
{
    public interface ICryptoGraphy
    {
        public (string Key, string IVBase64) InitSymmetricEncryptionKeyIV();
        string Encrypt(string text, string IV, string key);
        string Decrypt(string encryptedText, string IV, string key);
    }
}

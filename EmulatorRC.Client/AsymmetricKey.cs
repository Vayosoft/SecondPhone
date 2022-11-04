using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EmulatorRC.Client
{
    public static class AsymmetricKey
    {
        public static void Create()
        {
            using var rsa = RSA.Create();
            Console.WriteLine($"-----Private key-----{Environment.NewLine}{Convert.ToBase64String(rsa.ExportRSAPrivateKey())}{Environment.NewLine}");
            Console.WriteLine($"-----Public key-----{Environment.NewLine}{Convert.ToBase64String(rsa.ExportRSAPublicKey())}");
        }
    }
}

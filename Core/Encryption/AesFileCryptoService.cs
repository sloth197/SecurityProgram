using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecurityProgram.App.Core.Encryption
{
    public class AesFileCryptoService
    {
        private const int KeySize = 256;
        private const int Iterations = 100_000;
        public void EncryptFile(string inputFile, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            
            var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = key.GetBytes(32);
            aes.GenerateIV();
            
            var outputFile = inputFile + ".enc";
            using var fsOut = new FileStream(outputFile, FileMode.Create);
            fsOut.Write(salt, 0, salt.Length);
            fsOut.Write(aes.IV, 0, aes.IV.Length);

            using var cryptoStream = new CryptoStream(
                fsOut,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write
            );

            using var fsIn - new FileStream(inputFile, FileMode.Open);
            fsIn.CopyTo(cryptoStream);
        }

        public void DecryptFile(string inputFile, string password)
        {
            using var fsIn = new FileStream(inputFile, FileMode.Open);
            var salt = new byte[16];
            var iv = new byte[16];
            fsIn.Read(salt, 0, salt.Length);
            fsIn.Read(iv, 0, iv.Length);

            using var fsOut new FileStream(outputFile, FileMode.Create);
            using var cryptoStream = new CryptoStream(
                fsIn,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read
            );
            cryptoStream.CopyTo(fsOut);
        }
    }
}
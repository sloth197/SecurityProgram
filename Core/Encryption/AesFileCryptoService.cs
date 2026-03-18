using System;
using System.IO;
using System.Security.Cryptography;

namespace SecurityProgram.App.Core.Encryption;

public class AesFileCryptoService
{
    private const int KeySizeBits = 256;
    private const int Iterations = 100_000;

    public string EncryptFile(string inputFile, string password)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("Input file was not found.", inputFile);
        }

        var outputFile = inputFile + ".enc";

        var salt = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.KeySize = KeySizeBits;
        aes.GenerateIV();

        using var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(aes.KeySize / 8);

        using var fsIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fsOut = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);

        fsOut.Write(salt, 0, salt.Length);
        fsOut.Write(aes.IV, 0, aes.IV.Length);

        using var cryptoStream = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write);
        fsIn.CopyTo(cryptoStream);

        return outputFile;
    }

    public string DecryptFile(string inputFile, string password)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("Input file was not found.", inputFile);
        }

        string outputFile = inputFile.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
            ? inputFile[..^4]
            : inputFile + ".dec";

        using var fsIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

        var salt = new byte[16];
        var iv = new byte[16];

        if (fsIn.Read(salt, 0, salt.Length) != salt.Length || fsIn.Read(iv, 0, iv.Length) != iv.Length)
        {
            throw new InvalidDataException("Input file format is invalid.");
        }

        using var aes = Aes.Create();
        aes.KeySize = KeySizeBits;
        aes.IV = iv;

        using var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(aes.KeySize / 8);

        using var fsOut = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        using var cryptoStream = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read);
        cryptoStream.CopyTo(fsOut);

        return outputFile;
    }
}

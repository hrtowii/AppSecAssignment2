using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Assignment2.Services
{
    public class EncryptionService
    {
        private readonly byte[] _key;
        private readonly int _passwordHashIterations = 100000;

        public EncryptionService(IConfiguration configuration)
        {
            var encryptionKey = configuration["EncryptionKey"];
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
        }

        // Encrypt data (e.g., credit card) with AES
        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // New IV for each encryption

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length); // Prepend IV to ciphertext
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _key;

            // Extract IV from cipher bytes
            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(cipherBytes, iv, iv.Length);
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        // Hash password with PBKDF2
        public string HashPassword(string password, out string salt)
        {
            salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128 / 8));
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(salt),
                _passwordHashIterations,
                HashAlgorithmName.SHA512,
                256 / 8
            );
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(salt),
                _passwordHashIterations,
                HashAlgorithmName.SHA512,
                256 / 8
            );
            return computedHash.SequenceEqual(Convert.FromBase64String(hash));
        }
    }
}
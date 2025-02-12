using System;
using System.Security.Cryptography;
using System.Text;

namespace Assignment2.Services
{
    public class EncryptionService
    {
        // Basic example of encryption; for real applications, use a more robust mechanism.
        public static string Encrypt(string plainText)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
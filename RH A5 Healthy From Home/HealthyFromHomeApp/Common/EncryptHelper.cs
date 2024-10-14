using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HealthyFromHomeApp.Common
{
    internal class EncryptHelper
    {
        //hardcoded voor nu
        private static readonly string encryptionKey = "A54Lifepadpadpad"; 

        public static string Encrypt(string plainText)
        {
            byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
            byte[] iv;

            using (Aes aes = Aes.Create())
            {
                Console.WriteLine(key);
                aes.Key = key;
                aes.GenerateIV();
                iv = aes.IV; 

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(plainText);
                        }
                    }

                    byte[] encryptedContent = memoryStream.ToArray();

                    byte[] result = new byte[iv.Length + encryptedContent.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
            Console.Write(encryptedText);
            byte[] fullCipher = Convert.FromBase64String(encryptedText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;

                byte[] iv = new byte[16];
                byte[] cipherText = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                aes.IV = iv;

                using (MemoryStream memoryStream = new MemoryStream(cipherText))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cryptoStream))
                        {
                            string decrypted = reader.ReadToEnd();
                            return decrypted;
                        }
                    }
                }
            }
        }
    }
}

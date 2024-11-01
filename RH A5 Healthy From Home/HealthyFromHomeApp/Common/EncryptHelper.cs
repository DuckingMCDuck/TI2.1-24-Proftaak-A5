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

        // AES encryption
        public static string Encrypt(string plainText)
        {
            // Convert encryption key to byte array
            byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
            byte[] iv; // Initialization vector

            using (Aes aes = Aes.Create())
            {
                Console.WriteLine("ENCRYPT: " + key);
                aes.Key = key;
                aes.GenerateIV(); 
                iv = aes.IV; 

                // Holds encrypted bytes
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // Encrypts and writes data to the memorystream
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // write plain text to cryptostream
                        using (StreamWriter writer = new StreamWriter(cryptoStream))
                        { 
                            writer.Write(plainText);
                        }
                    }

                    // retrieve encrypted data from memorystream
                    byte[] encryptedContent = memoryStream.ToArray();

                    // combine the IV and encrypted content into single byte array
                    byte[] result = new byte[iv.Length + encryptedContent.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length); // copy IV at beginning
                    Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length); //copy after IV is added

                    // Return combined IV and encrypted content as Base64
                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            // convert encryption key to byte array
            byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
            byte[] fullCipher = Convert.FromBase64String(encryptedText);

            // create new instance of AES algorithm
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;

                // first 16 bytes of the cipher are the IV
                byte[] iv = new byte[16];
                byte[] cipherText = new byte[fullCipher.Length - iv.Length];

                // extract IV from full cipher
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                // extract actual message
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

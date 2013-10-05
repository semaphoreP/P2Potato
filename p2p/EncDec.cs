using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace P2Potato {
    class EncDec {
        private static byte[] salt = new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65,
            0x76, 0x84, 0x82, 0x85, 0x85, 0x17
        };

        /// <summary>
        /// Encrypt a byte array using a key and an IV
        /// </summary>
        /// <param name="clearData"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV) {
            MemoryStream ms = new MemoryStream();
            Aes alg = Aes.Create();
            alg.Key = Key;
            alg.IV = IV;
            CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearData, 0, clearData.Length);
            cs.Close();
            byte[] encryptedData = ms.ToArray();

            return encryptedData;
        }

        /// <summary>
        /// Encrypt a string using a password
        /// See Encrypt(byte[], byte[], byte[])
        /// </summary>
        /// <param name="clearText"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Encrypt(string clearText, string password) {
            byte[] clearBytes = System.Text.Encoding.UTF8.GetBytes(clearText);
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);
            byte[] encryptedData = Encrypt(clearBytes, rdb.GetBytes(32), rdb.GetBytes(16));
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Encrypt bytes using a password
        /// See Encrypt(byte[], byte[], byte[])
        /// </summary>
        /// <param name="clearData"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] clearData, string password) {
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);
            return Encrypt(clearData, rdb.GetBytes(32), rdb.GetBytes(16));
        }

        /// <summary>
        /// Encrypt a file using a password
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        public static void Encrypt(string fileIn, string fileOut, string password) {
            FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);

            Aes alg = Aes.Create();
            alg.Key = rdb.GetBytes(32);
            alg.IV = rdb.GetBytes(16);
            CryptoStream cs = new CryptoStream(fsOut, alg.CreateEncryptor(), CryptoStreamMode.Write);
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do {
                bytesRead = fsIn.Read(buffer, 0, bufferLen);
                cs.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);
            cs.Close();
            fsIn.Close();
        }

        /// <summary>
        /// Decrypt a byte array using a key and an IV
        /// </summary>
        /// <param name="cipherData"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV) {
            MemoryStream ms = new MemoryStream();
            Aes alg = Aes.Create();
            alg.Key = Key;
            alg.IV = IV;
            CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherData, 0, cipherData.Length);
            cs.Close();
            byte[] decryptedData = ms.ToArray();
            return decryptedData;
        }

        /// <summary>
        /// Decrypt a string using a password
        /// See Decrypt(byte[], byte[], byte[])
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Decrypt(string cipherText, string password) {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);
            byte[] decryptedData = Decrypt(cipherBytes, rdb.GetBytes(32), rdb.GetBytes(16));
            return System.Text.Encoding.UTF8.GetString(decryptedData);
        }

        /// <summary>
        /// Decrypt bytes using a password
        /// See Decrypt(byte[], byte[], byte[])
        /// </summary>
        /// <param name="cipherData"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] cipherData, string password) {
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);
            return Decrypt(cipherData, rdb.GetBytes(32), rdb.GetBytes(16));
            //return Encoding.ASCII.GetBytes(DecryptStringFromBytes(cipherData, pdb.GetBytes(32), pdb.GetBytes(16)));
        }

        /// <summary>
        /// Decrypt a file using a password
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        public static void Decrypt(string fileIn, string fileOut, string password) {
            FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(password, salt);
            Aes alg = Aes.Create();
            alg.Key = rdb.GetBytes(32);
            alg.IV = rdb.GetBytes(16);
            CryptoStream cs = new CryptoStream(fsOut, alg.CreateDecryptor(), CryptoStreamMode.Write);
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;
            do {
                bytesRead = fsIn.Read(buffer, 0, bufferLen);
                cs.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);
            cs.Close();
            fsIn.Close();
        }

        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV) {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            using (Aes alg = Aes.Create()) {
                alg.Key = Key;
                alg.IV = IV;
                ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);
                using (MemoryStream msEncrypt = new MemoryStream()) {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV) {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            string plaintext = null;
            using (Aes alg = Aes.Create()) {
                alg.Key = Key;
                alg.IV = IV;
                ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }
}
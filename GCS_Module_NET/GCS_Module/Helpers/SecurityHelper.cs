using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GCS_Module.Helpers
{
    /// <summary>
    /// AES-256 CBC modunda şifre çözme işlemleri
    /// Python tarafıyla tam uyumlu
    /// </summary>
    public class SecurityHelper
    {
        private readonly byte[] _key;

        // ============================================================
        // CONSTRUCTOR (Dependency Injection ile anahtar yükleme)
        // ============================================================
        public SecurityHelper(IConfiguration configuration)
        {
            // appsettings.json'dan anahtar oku
            var keyString = configuration["Security:AesKey"];

            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException(
                    "AES anahtarı bulunamadı! appsettings.json'da 'Security:AesKey' tanımlayın."
                );
            }

            _key = Encoding.UTF8.GetBytes(keyString);

            // AES-256 için tam 32 byte kontrolü
            if (_key.Length != 32)
            {
                throw new ArgumentException(
                    $"AES-256 anahtarı 32 byte olmalı! Mevcut: {_key.Length} byte"
                );
            }
        }

        // ============================================================
        // STATIC METHOD (Geriye dönük uyumluluk için)
        // ============================================================
        private static byte[] DefaultKey =>
            Encoding.UTF8.GetBytes("SVT_Sistemi_Gizli_Anahtar_32Byte");

        /// <summary>
        /// Base64 encoded şifreli metni çözer
        /// </summary>
        /// <param name="cipherText">Base64 encoded IV + ciphertext</param>
        /// <returns>Çözülmüş JSON string</returns>
        public string Decrypt(string cipherText)
        {
            return DecryptInternal(cipherText, _key);
        }

        /// <summary>
        /// Static decrypt metodu (DI olmadan kullanım için)
        /// ÖNEMLİ: Üretim ortamında DI ile Decrypt() kullanın!
        /// </summary>
        public static string DecryptStatic(string cipherText)
        {
            return DecryptInternal(cipherText, DefaultKey);
        }

        // ============================================================
        // INTERNAL DECRYPT LOGIC
        // ============================================================
        private static string DecryptInternal(string cipherText, byte[] key)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                throw new ArgumentException("Şifreli metin boş olamaz!", nameof(cipherText));
            }

            try
            {
                // 1. Base64 decode (Python: base64.b64decode)
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                if (fullCipher.Length < 16)
                {
                    throw new ArgumentException("Geçersiz şifreli veri (çok kısa)");
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;      // Python: AES.MODE_CBC
                    aes.Padding = PaddingMode.PKCS7; // Python: pad(..., AES.block_size)

                    // 2. IV'yi ayır (ilk 16 byte)
                    byte[] iv = new byte[16];
                    Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);

                    // 3. Ciphertext'i ayır (geri kalan)
                    byte[] cipher = new byte[fullCipher.Length - 16];
                    Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

                    aes.IV = iv;

                    // 4. Şifreyi çöz
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Base64 decode hatası. Veri bozuk olabilir.", ex
                );
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Şifre çözme hatası. Anahtar yanlış veya veri bozuk.", ex
                );
            }
        }

        // ============================================================
        // VALIDATION HELPER
        // ============================================================
        public static bool ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var bytes = Encoding.UTF8.GetBytes(key);
            return bytes.Length == 32;
        }
    }
}
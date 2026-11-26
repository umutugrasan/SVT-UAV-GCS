using System;
using System.Security.Cryptography;
using System.Text;

namespace GCS_Module.Helpers
{
    /// <summary>
    /// Modern şifre hash'leme: PBKDF2 (HMAC-SHA256), kullanıcıya özel rastgele tuz.
    /// Depolama biçimi:  pbkdf2$<iterasyon>$<base64Tuz>$<base64Hash>
    ///
    /// Eski kayıtlarla (düz SHA256 + sabit tuz) geriye dönük uyumludur:
    /// VerifyPassword eski biçimi de doğrular, böylece mevcut kullanıcılar
    /// giriş yapmaya devam eder ve ilk başarılı girişte yeni biçime yükseltilir.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;        // 128 bit
        private const int KeySize = 32;         // 256 bit
        private const int Iterations = 100_000; // PBKDF2 tur sayısı
        private const string Prefix = "pbkdf2";

        // Eski (legacy) biçimle uyum için — AccountController'daki eski değerle aynı olmalı.
        private const string LegacySalt = "SVT_SALT_2024";

        /// <summary>
        /// Düz metin şifreden yeni biçimde (PBKDF2) hash üretir.
        /// </summary>
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verilen şifreyi depolanan hash ile karşılaştırır.
        /// Yeni (pbkdf2$...) ve eski (düz SHA256 base64) biçimlerin ikisini de destekler.
        /// </summary>
        /// <param name="password">Kullanıcının girdiği düz metin şifre</param>
        /// <param name="storedHash">Veritabanındaki PasswordHash</param>
        /// <param name="needsUpgrade">Eski biçim doğrulandıysa true döner — çağıran taraf yeni biçimle güncellemelidir.</param>
        public static bool VerifyPassword(string password, string storedHash, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrEmpty(storedHash))
                return false;

            // Yeni biçim: pbkdf2$<iter>$<salt>$<hash>
            if (storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4)
                    return false;

                if (!int.TryParse(parts[1], out int iterations))
                    return false;

                byte[] salt, expected;
                try
                {
                    salt = Convert.FromBase64String(parts[2]);
                    expected = Convert.FromBase64String(parts[3]);
                }
                catch (FormatException)
                {
                    return false;
                }

                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            // Eski biçim: Base64(SHA256(password + LegacySalt))
            if (VerifyLegacy(password, storedHash))
            {
                needsUpgrade = true;
                return true;
            }

            return false;
        }

        private static bool VerifyLegacy(string password, string storedHash)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + LegacySalt));
            var legacyHash = Convert.ToBase64String(bytes);

            var a = Encoding.UTF8.GetBytes(legacyHash);
            var b = Encoding.UTF8.GetBytes(storedHash);
            if (a.Length != b.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}

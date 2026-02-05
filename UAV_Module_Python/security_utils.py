import json
import base64
import os
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from dotenv import load_dotenv

# .env dosyasından anahtarı yükle
load_dotenv()

# Anahtar yönetimi (Güvenli)
SECRET_KEY = os.getenv('AES_SECRET_KEY')
if SECRET_KEY is None:
    raise ValueError(
        "❌ AES_SECRET_KEY bulunamadı! "
        ".env dosyasında 'AES_SECRET_KEY=SVT_Sistemi_Gizli_Anahtar_32Byte' tanımlayın"
    )

# Byte formatına çevir
SECRET_KEY = SECRET_KEY.encode('utf-8')

# Anahtar uzunluğu kontrolü (AES-256 için 32 byte gerekli)
if len(SECRET_KEY) != 32:
    raise ValueError(
        f"❌ AES-256 için anahtar 32 byte olmalı! "
        f"Mevcut: {len(SECRET_KEY)} byte"
    )


def encrypt_target_data(json_string):
    """
    Gelen herhangi bir JSON string verisini AES-256 CBC modunda şifreler.
    
    Args:
        json_string (str): Şifrelenecek JSON formatındaki veri (string)
        
    Returns:
        str: Base64 encoded şifreli veri (IV + ciphertext)
        
    Raises:
        ValueError: json_string boş veya None ise
        TypeError: json_string string değilse
        
    Example:
        >>> data = '{"type": "car", "coords": [100, 200]}'
        >>> encrypted = encrypt_target_data(data)
        >>> print(encrypted)
        'kJ8vN2xQ...'  # Base64 encoded IV + ciphertext
    """
    # Input validasyonu
    if json_string is None or json_string == "":
        raise ValueError("❌ Şifrelenecek veri boş olamaz!")
    
    if not isinstance(json_string, str):
        raise TypeError(f"❌ Beklenen tip: str, gelen: {type(json_string).__name__}")
    
    try:
        # JSON geçerliliğini kontrol et
        json.loads(json_string)
    except json.JSONDecodeError as e:
        raise ValueError(f"❌ Geçersiz JSON formatı: {e}")
    
    # Şifreleyiciyi oluştur (CBC modu)
    cipher = AES.new(SECRET_KEY, AES.MODE_CBC)
    iv = cipher.iv  # Rastgele 16 byte IV (Initialization Vector)
    
    # Veriyi şifrele ve PKCS7 padding uygula
    plaintext_bytes = json_string.encode('utf-8')
    padded_plaintext = pad(plaintext_bytes, AES.block_size)
    encrypted_bytes = cipher.encrypt(padded_plaintext)
    
    # IV + Şifreli Metin birleştir ve Base64'e çevir
    # C# tarafında önce 16 byte IV, sonra ciphertext olarak parse edilecek
    combined_data = iv + encrypted_bytes
    encoded_final = base64.b64encode(combined_data).decode('utf-8')
    
    return encoded_final


def validate_key():
    """
    Anahtar yönetiminin doğru yapıldığını kontrol eder.
    Program başlangıcında çağrılmalıdır.
    
    Returns:
        bool: Anahtar geçerliyse True
        
    Raises:
        ValueError: Anahtar problemli ise
    """
    if SECRET_KEY is None:
        raise ValueError("❌ Şifreleme anahtarı yüklenemedi!")
    
    if len(SECRET_KEY) != 32:
        raise ValueError(f"❌ AES-256 anahtarı 32 byte olmalı! (Mevcut: {len(SECRET_KEY)})")
    
    print(f"✅ Şifreleme anahtarı doğrulandı ({len(SECRET_KEY)} byte)")
    return True


# Test fonksiyonu (sadece development için)
if __name__ == "__main__":
    print("🔐 Şifreleme Modülü Test Ediliyor...\n")
    
    # Anahtar validasyonu
    try:
        validate_key()
    except ValueError as e:
        print(e)
        exit(1)
    
    # Örnek veri (Hatay koordinatları ile)
    test_data = {
        "id": "test-123",
        "type": "car",
        "pixelCoords": "[450, 320]",           # ✅ String format
        "gpsCoords": "[36.2017, 36.1611]",     # ✅ Hatay koordinatları
        "confidence": 0.87,
        "timestamp": "2025-01-22T14:35:20",
        "frameId": 1
    }
    
    json_str = json.dumps(test_data)
    print(f"📦 Orijinal Veri ({len(json_str)} byte):")
    print(json_str)
    print()
    
    # Şifreleme
    encrypted = encrypt_target_data(json_str)
    print(f"🔒 Şifreli Veri ({len(encrypted)} karakter):")
    print(encrypted)
    print()
    
    # IV ve Ciphertext ayrıştırma (C# tarafı için bilgi)
    decoded = base64.b64decode(encrypted)
    iv_extracted = decoded[:16]
    ciphertext = decoded[16:]
    print(f"📋 Paket Yapısı:")
    print(f"  - IV: {len(iv_extracted)} byte")
    print(f"  - Ciphertext: {len(ciphertext)} byte")
    print(f"  - Toplam: {len(decoded)} byte")
    print()
    
    print("✅ Test başarılı!")
    print(f"🗺️ GPS Koordinatları: Hatay/Antakya (36.2017, 36.1611)")
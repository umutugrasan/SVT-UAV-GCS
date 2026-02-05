"""
SVT (Secure-Vision Telemetry) Sistem Konfigürasyonu
Tüm sistem ayarları bu dosyada merkezi olarak yönetilir.
"""

import os
from dotenv import load_dotenv

# .env dosyasını yükle
load_dotenv()


class Config:
    """
    Sistem genelinde kullanılacak tüm konfigürasyon parametreleri
    """
    
    # ============================================================
    # UAV KİMLİK AYARLARI (YENİ)
    # ============================================================
    UAV_ID = os.getenv('UAV_ID', 'UAV-001')  # Benzersiz UAV kimliği
    UAV_NAME = os.getenv('UAV_NAME', 'Alpha-1')  # UAV görünen adı
    
    # ============================================================
    # YOLO MODEL AYARLARI
    # ============================================================
    MODEL_PATH = 'yolov8s.pt'  # YOLOv8 model dosyası (s=small, m=medium, l=large)
    CONFIDENCE_THRESHOLD = 0.25  # Minimum tespit güveni (0-1 arası)
    IMAGE_SIZE = 640  # YOLO için input görüntü boyutu
    
    # ============================================================
    # NETWORK AYARLARI
    # ============================================================
    SERVER_IP = os.getenv('GCS_SERVER_IP', '127.0.0.1')  # Yer Kontrol İstasyonu IP
    SERVER_PORT = int(os.getenv('GCS_SERVER_PORT', '5050'))  # TCP Port
    CONNECTION_TIMEOUT = 5  # Saniye cinsinden bağlantı zaman aşımı
    RETRY_ATTEMPTS = 3  # Başarısız bağlantıda kaç kez deneme yapılacak
    RETRY_DELAY = 1.0  # Denemeler arası bekleme süresi (saniye)
    
    # ============================================================
    # BATCH PROCESSING AYARLARI
    # ============================================================
    BATCH_INTERVAL = 1.0  # Batch gönderim aralığı (saniye)
    MAX_BATCH_SIZE = 50  # Bir batch'te maksimum hedef sayısı
    
    # ============================================================
    # GPS SİMÜLASYON AYARLARI
    # ============================================================
    # Hatay koordinatları (varsayılan uçuş merkezi)
    BASE_LATITUDE = float(os.getenv('BASE_LATITUDE', '36.2017'))
    BASE_LONGITUDE = float(os.getenv('BASE_LONGITUDE', '36.1611'))
    FLIGHT_RADIUS = 0.05  # Simülasyon yarıçapı (derece cinsinden, ~5km)
    
    # ============================================================
    # VİDEO AYARLARI
    # ============================================================
    DEFAULT_VIDEO = 'test_video.mp4'  # Varsayılan test videosu
    DISPLAY_WIDTH = 1024  # Görüntü penceresi genişliği
    DISPLAY_HEIGHT = 768  # Görüntü penceresi yüksekliği
    
    # ============================================================
    # LOGGING AYARLARI
    # ============================================================
    LOG_FILE = 'uav_module.log'  # Log dosya adı
    LOG_LEVEL = os.getenv('LOG_LEVEL', 'INFO')  # DEBUG, INFO, WARNING, ERROR
    LOG_FORMAT = '%(asctime)s - [%(levelname)s] - %(message)s'
    
    # ============================================================
    # GÜVENLİK AYARLARI
    # ============================================================
    # AES anahtarı .env dosyasından okunur (security_utils.py içinde kullanılıyor)
    AES_KEY_LENGTH = 32  # AES-256 için 32 byte gerekli
    
    # ============================================================
    # PERFORMANS AYARLARI
    # ============================================================
    FPS_UPDATE_INTERVAL = 1.0  # FPS güncelleme aralığı (saniye)
    ENABLE_GPU = True  # CUDA varsa GPU kullan
    
    # ============================================================
    # DEBUG MODU
    # ============================================================
    DEBUG_MODE = os.getenv('DEBUG_MODE', 'False').lower() == 'true'
    VERBOSE_LOGGING = os.getenv('VERBOSE_LOGGING', 'False').lower() == 'true'
    
    @classmethod
    def validate(cls):
        """
        Konfigürasyon ayarlarının geçerliliğini kontrol eder.
        Programın başında çağrılmalıdır.
        """
        errors = []
        
        # Model dosyası kontrolü
        if not os.path.exists(cls.MODEL_PATH):
            errors.append(f"❌ Model dosyası bulunamadı: {cls.MODEL_PATH}")
        
        # Confidence threshold kontrolü
        if not 0 <= cls.CONFIDENCE_THRESHOLD <= 1:
            errors.append(f"❌ CONFIDENCE_THRESHOLD 0-1 arasında olmalı! (Mevcut: {cls.CONFIDENCE_THRESHOLD})")
        
        # Port kontrolü
        if not 1024 <= cls.SERVER_PORT <= 65535:
            errors.append(f"❌ SERVER_PORT 1024-65535 arasında olmalı! (Mevcut: {cls.SERVER_PORT})")
        
        # GPS koordinat kontrolü
        if not -90 <= cls.BASE_LATITUDE <= 90:
            errors.append(f"❌ BASE_LATITUDE -90 ile 90 arasında olmalı! (Mevcut: {cls.BASE_LATITUDE})")
        
        if not -180 <= cls.BASE_LONGITUDE <= 180:
            errors.append(f"❌ BASE_LONGITUDE -180 ile 180 arasında olmalı! (Mevcut: {cls.BASE_LONGITUDE})")
        
        # UAV ID kontrolü
        if not cls.UAV_ID:
            errors.append("❌ UAV_ID boş olamaz!")
        
        # Hata varsa exception fırlat
        if errors:
            raise ValueError("\n".join(errors))
        
        print("✅ Tüm konfigürasyon ayarları geçerli")
        return True
    
    @classmethod
    def print_config(cls):
        """
        Aktif konfigürasyonu ekrana yazdırır (debug için)
        """
        print("\n" + "="*60)
        print("🔧 SVT SİSTEM KONFIGÜRASYONU")
        print("="*60)
        print(f"🚁 UAV ID: {cls.UAV_ID}")
        print(f"🚁 UAV Name: {cls.UAV_NAME}")
        print(f"Model: {cls.MODEL_PATH}")
        print(f"Confidence Threshold: {cls.CONFIDENCE_THRESHOLD}")
        print(f"GCS Server: {cls.SERVER_IP}:{cls.SERVER_PORT}")
        print(f"Batch Interval: {cls.BATCH_INTERVAL}s")
        print(f"Max Batch Size: {cls.MAX_BATCH_SIZE}")
        print(f"GPS Center: ({cls.BASE_LATITUDE}, {cls.BASE_LONGITUDE})")
        print(f"Debug Mode: {cls.DEBUG_MODE}")
        print("="*60 + "\n")


# Test/Debug için
if __name__ == "__main__":
    print("🧪 Config Dosyası Test Ediliyor...\n")
    
    try:
        Config.validate()
        Config.print_config()
        print("✅ Test başarılı!")
    except ValueError as e:
        print(e)
        exit(1)
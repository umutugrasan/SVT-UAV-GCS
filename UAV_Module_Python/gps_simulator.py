"""
GPS Simulator - Pixel Koordinatlarını GPS'e Dönüştürür
Gerçek bir GPS modülü olmadan harita entegrasyonu için koordinat üretir.
"""

import random
import math


class GPSSimulator:
    """
    Video frame'indeki pixel koordinatlarını GPS koordinatlarına çevirir.
    Gerçek bir drone uçuşunu simüle eder.
    """
    
    def __init__(self, base_lat=36.2017, base_lon=36.1611, flight_radius=0.05):
        """
        Args:
            base_lat (float): Merkez nokta enlem (latitude)
            base_lon (float): Merkez nokta boylam (longitude)
            flight_radius (float): Uçuş yarıçapı (derece cinsinden, ~5km için 0.05)
        """
        self.base_lat = base_lat
        self.base_lon = base_lon
        self.flight_radius = flight_radius
        
        # Drone hareketi simülasyonu için
        self.current_heading = random.uniform(0, 360)  # Başlangıç yönü (derece)
        self.speed = 0.0001  # Saniyedeki hareket hızı (derece/saniye)
        
        print(f"🌍 GPS Simulator başlatıldı: ({base_lat:.6f}, {base_lon:.6f})")
        print(f"   Uçuş Yarıçapı: ~{flight_radius * 111:.1f} km")
    
    def get_coordinates(self, pixel_x, pixel_y, frame_width, frame_height):
        """
        Pixel koordinatlarını GPS koordinatlarına çevirir.
        
        Args:
            pixel_x (int): X koordinatı (0 - frame_width)
            pixel_y (int): Y koordinatı (0 - frame_height)
            frame_width (int): Frame genişliği (pixel)
            frame_height (int): Frame yüksekliği (pixel)
            
        Returns:
            tuple: (latitude, longitude)
            
        Example:
            >>> gps = GPSSimulator()
            >>> lat, lon = gps.get_coordinates(640, 360, 1280, 720)
            >>> print(f"GPS: {lat:.6f}, {lon:.6f}")
            GPS: 39.933400, 32.859700
        """
        # Pixel koordinatlarını normalize et (0-1 arası)
        norm_x = pixel_x / frame_width
        norm_y = pixel_y / frame_height
        
        # Merkeze göre offset hesapla (-0.5 ile +0.5 arası)
        offset_x = (norm_x - 0.5) * self.flight_radius
        offset_y = (norm_y - 0.5) * self.flight_radius
        
        # GPS koordinatlarını hesapla
        # Not: Y ekseninde ters çevirme (pixel'de yukarı = küçük Y, GPS'te yukarı = büyük lat)
        latitude = self.base_lat - offset_y
        longitude = self.base_lon + offset_x
        
        # Küçük rastgelelik ekle (sensör gürültüsü simülasyonu)
        latitude += random.gauss(0, 0.00001)   # ~1 metre hassasiyet
        longitude += random.gauss(0, 0.00001)
        
        return latitude, longitude
    
    def get_moving_coordinates(self, pixel_x, pixel_y, frame_width, frame_height, time_delta=0.033):
        """
        Hareket eden bir drone simüle eder (gelişmiş versiyon).
        Her çağrıda drone'un pozisyonu güncellenir.
        
        Args:
            pixel_x, pixel_y, frame_width, frame_height: get_coordinates ile aynı
            time_delta (float): Son çağrıdan beri geçen süre (saniye)
            
        Returns:
            tuple: (latitude, longitude)
        """
        # Yönü rastgele değiştir (rüzgar etkisi simülasyonu)
        self.current_heading += random.gauss(0, 2)  # ±2 derece
        self.current_heading = self.current_heading % 360
        
        # Hızı rastgele değiştir
        self.speed += random.gauss(0, 0.00001)
        self.speed = max(0.00005, min(0.0002, self.speed))  # Limit: 0.00005 - 0.0002
        
        # Hareket vektörünü hesapla
        heading_rad = math.radians(self.current_heading)
        lat_change = math.cos(heading_rad) * self.speed * time_delta
        lon_change = math.sin(heading_rad) * self.speed * time_delta
        
        # Base pozisyonu güncelle
        self.base_lat += lat_change
        self.base_lon += lon_change
        
        # Pixel koordinatlarını GPS'e çevir
        return self.get_coordinates(pixel_x, pixel_y, frame_width, frame_height)
    
    def get_stationary_coordinates(self, pixel_x, pixel_y, frame_width, frame_height):
        """
        Sabit hovering drone simüle eder (basit versiyon).
        Kamera sabit, sadece görüntü içindeki hedefler hareket eder.
        
        Returns:
            tuple: (latitude, longitude)
        """
        return self.get_coordinates(pixel_x, pixel_y, frame_width, frame_height)
    
    def reset_position(self, new_lat=None, new_lon=None):
        """
        Drone pozisyonunu sıfırlar.
        
        Args:
            new_lat (float, optional): Yeni merkez enlem
            new_lon (float, optional): Yeni merkez boylam
        """
        if new_lat is not None:
            self.base_lat = new_lat
        if new_lon is not None:
            self.base_lon = new_lon
        
        self.current_heading = random.uniform(0, 360)
        print(f"🔄 GPS pozisyonu sıfırlandı: ({self.base_lat:.6f}, {self.base_lon:.6f})")
    
    def get_distance_meters(self, lat1, lon1, lat2, lon2):
        """
        İki GPS koordinatı arasındaki mesafeyi hesaplar (Haversine formülü).
        
        Args:
            lat1, lon1: Birinci nokta (derece)
            lat2, lon2: İkinci nokta (derece)
            
        Returns:
            float: Mesafe (metre)
        """
        R = 6371000  # Dünya yarıçapı (metre)
        
        phi1 = math.radians(lat1)
        phi2 = math.radians(lat2)
        delta_phi = math.radians(lat2 - lat1)
        delta_lambda = math.radians(lon2 - lon1)
        
        a = math.sin(delta_phi/2)**2 + \
            math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda/2)**2
        c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))
        
        distance = R * c
        return distance
    
    def validate_coordinates(self, lat, lon):
        """
        GPS koordinatlarının geçerliliğini kontrol eder.
        
        Returns:
            bool: Koordinatlar geçerliyse True
        """
        if not -90 <= lat <= 90:
            return False
        if not -180 <= lon <= 180:
            return False
        return True


# Test/Debug için
if __name__ == "__main__":
    print("🧪 GPS Simulator Test Ediliyor...\n")
    
    # Simulator oluştur (Ankara merkez)
    gps = GPSSimulator(base_lat=36.2017, base_lon=36.1611, flight_radius=0.05)
    
    # Test 1: Frame'in 4 köşesi
    print("📍 Test 1: Frame Köşeleri (1920x1080)")
    print("-" * 60)
    corners = [
        (0, 0, "Sol Üst"),
        (1920, 0, "Sağ Üst"),
        (0, 1080, "Sol Alt"),
        (1920, 1080, "Sağ Alt"),
        (960, 540, "Merkez")
    ]
    
    for x, y, label in corners:
        lat, lon = gps.get_coordinates(x, y, 1920, 1080)
        print(f"{label:12s}: ({x:4d}, {y:4d}) → GPS: ({lat:.6f}, {lon:.6f})")
    
    print()
    
    # Test 2: Mesafe hesaplama
    print("📏 Test 2: Mesafe Hesaplama")
    print("-" * 60)
    lat1, lon1 = gps.get_coordinates(0, 0, 1920, 1080)
    lat2, lon2 = gps.get_coordinates(1920, 1080, 1920, 1080)
    distance = gps.get_distance_meters(lat1, lon1, lat2, lon2)
    print(f"Sol Üst → Sağ Alt mesafe: {distance:.2f} metre")
    print()
    
    # Test 3: Hareket simülasyonu
    print("🚁 Test 3: Hareket Simülasyonu (5 saniye)")
    print("-" * 60)
    print(f"{'Zaman':8s} | {'Latitude':12s} | {'Longitude':12s} | {'Heading':8s}")
    print("-" * 60)
    
    gps.reset_position()
    for i in range(5):
        lat, lon = gps.get_moving_coordinates(960, 540, 1920, 1080, time_delta=1.0)
        print(f"{i+1:4d}s    | {lat:12.6f} | {lon:12.6f} | {gps.current_heading:7.2f}°")
    
    print()
    
    # Test 4: Validasyon
    print("✅ Test 4: Koordinat Validasyonu")
    print("-" * 60)
    test_coords = [
        (39.9334, 32.8597, True),
        (91.0, 32.8597, False),   # Geçersiz latitude
        (39.9334, 181.0, False),  # Geçersiz longitude
    ]
    
    for lat, lon, expected in test_coords:
        result = gps.validate_coordinates(lat, lon)
        status = "✅" if result == expected else "❌"
        print(f"{status} ({lat:.1f}, {lon:.1f}): {result}")
    
    print("\n✅ Tüm testler tamamlandı!")
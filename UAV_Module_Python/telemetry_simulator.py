"""
Telemetry Simulator - Gerçekçi İHA Sensör Verisi Üretimi
"""

import random
import time


class TelemetrySimulator:
    """Gerçekçi İHA telemetri simülasyonu"""
    
    def __init__(self):
        # Başlangıç değerleri
        self.battery = 100.0
        self.altitude = 0.0
        self.speed = 0.0
        self.heading = 0.0
        self.signal_strength = 100
        
        # Uçuş durumu
        self.is_flying = False
        self.flight_start_time = None
        self.target_altitude = 150.0
        
        # Simülasyon parametreleri
        self.battery_drain_rate = 0.02  # %/saniye (daha yavaş)
        self.climb_rate = 10.0  # m/s
        self.max_speed = 80.0  # km/h
        
        print("📡 Telemetry Simulator başlatıldı")
        
    def start_flight(self):
        """Uçuşu başlat"""
        self.is_flying = True
        self.flight_start_time = time.time()
        self.battery = 100.0
        print("🛫 Uçuş başladı")
        
    def update(self):
        """Her frame'de çağrılır, telemetri günceller"""
        if not self.is_flying:
            self.start_flight()
        
        # ===== BATARYA =====
        speed_factor = 1 + (self.speed / self.max_speed) * 0.5
        self.battery -= self.battery_drain_rate * speed_factor * 0.1
        self.battery = max(0, self.battery)
        
        # ===== İRTİFA =====
        if self.altitude < self.target_altitude:
            self.altitude += self.climb_rate * 0.05 + random.gauss(0, 0.2)
        else:
            self.altitude += random.gauss(0, 0.3)
        self.altitude = max(0, min(500, self.altitude))
        
        # ===== HIZ =====
        target_speed = random.uniform(45, 65)
        self.speed += (target_speed - self.speed) * 0.05
        self.speed += random.gauss(0, 1)
        self.speed = max(0, min(self.max_speed, self.speed))
        
        # ===== YÖN =====
        self.heading += random.gauss(0, 0.5)
        self.heading = self.heading % 360
        
        # ===== SİNYAL =====
        base_signal = 95 - (self.altitude / 500) * 20
        self.signal_strength = base_signal + random.gauss(0, 3)
        self.signal_strength = max(0, min(100, self.signal_strength))
        
    def get_data(self):
        """Güncel telemetri verisini döndür"""
        self.update()
        
        # Sinyal kalitesi string
        if self.signal_strength >= 80:
            signal_text = "Excellent"
        elif self.signal_strength >= 60:
            signal_text = "Good"
        elif self.signal_strength >= 40:
            signal_text = "Fair"
        elif self.signal_strength >= 20:
            signal_text = "Poor"
        else:
            signal_text = "Critical"
        
        return {
            "battery": round(self.battery, 1),
            "altitude": round(self.altitude, 1),
            "speed": round(self.speed, 1),
            "heading": round(self.heading, 1),
            "signal": signal_text,
            "signalStrength": round(self.signal_strength, 1),
            "isFlying": self.is_flying,
            "flightTime": round(time.time() - self.flight_start_time, 1) if self.flight_start_time else 0
        }


# Test
if __name__ == "__main__":
    print("🧪 Telemetry Simulator Test\n")
    
    sim = TelemetrySimulator()
    
    for i in range(10):
        data = sim.get_data()
        print(f"[{i+1}] 🔋{data['battery']:.1f}% | 📍{data['altitude']:.1f}m | 🚀{data['speed']:.1f}km/h | 🧭{data['heading']:.0f}° | 📶{data['signal']}")
        time.sleep(0.5)
    
    print("\n✅ Test tamamlandı!")




## Dosya Yapısı

# UAV_Module_Python/
# ├── uav_module.py          ← Güncellendi
# ├── telemetry_simulator.py ← YENİ
# ├── security_utils.py
# ├── gps_simulator.py
# ├── config.py
# └── .env
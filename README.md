# 🚁 SVT-UAV-GCS (Secure Vision Telemetry)

<div align="center">

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Status](https://img.shields.io/badge/status-In%20Development-yellow.svg)
![Tech](https://img.shields.io/badge/tech-C%23%20%7C%20Python%20%7C%20AI-purple.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![Python](https://img.shields.io/badge/Python-3.10+-3776AB.svg)
![YOLO](https://img.shields.io/badge/YOLOv8-Ultralytics-00FFFF.svg)

**⚠️ Bu proje aktif geliştirme aşamasındadır. Son hali değildir.**

[Türkçe](#-türkçe) | [English](#-english)

</div>

---

# 🇹🇷 TÜRKÇE

## 📋 Proje Hakkında

**SVT GCS**; **ASP.NET Core MVC** ve **Python** mimarisi üzerine inşa edilmiş, **Çoklu İHA (Multi-UAV)** desteğine sahip, gelişmiş ve güvenli bir İHA Yer Kontrol İstasyonudur.

Bu proje, görüntü işleme ve yapay zeka algoritmalarını **sanal ortam (virtual environment)** üzerinde izole ederek, sürüm uyuşmazlıklarının önüne geçmiş ve yüksek kararlılıkla çalışacak şekilde tasarlanmıştır.

---

## 🚀 Öne Çıkan Özellikler

### 🚁 Çoklu İHA Desteği (Multi-UAV Support)
Sistem, aynı anda birden fazla İHA'nın bağlanmasına, verilerinin ayrı ayrı işlenmesine ve yönetilmesine olanak tanır. Her İHA kendi kimliği (ID) ile takip edilir ve otomatik port ataması yapılır:
- `UAV-001` → Port 5050
- `UAV-002` → Port 5051
- `UAV-003` → Port 5052

### 📡 Gerçek Zamanlı Telemetri
İHA'lardan gelen irtifa, hız, batarya, konum ve sinyal verileri **SignalR** (WebSocket) teknolojisi ile gecikmesiz olarak arayüze aktarılır.

### 🧠 Yapay Zeka & Görüntü İşleme
- **Nesne Tespiti:** **YOLOv8 (Ultralytics)** algoritması ile araç, insan ve diğer nesneler anlık tespit edilir.
- **Plaka Tanıma (LPR):** **EasyOCR** derin öğrenme modelleri kullanılarak araç plakaları okunur ve arananlar listesiyle karşılaştırılır.
- **Tehdit Algılama:** Aranan plaka tespiti ve bölgesel yoğunluk analizi ile iki katmanlı tehdit sistemi.

### 🔒 Güvenli Veri İletimi
İHA ve Yer İstasyonu arasındaki tüm veri paketleri, **AES-256 CBC** şifreleme algoritması ile korunarak TCP soketleri üzerinden iletilir.

### 💻 Sanal Ortam Mimarisi
Python modülleri, kararlılık ve bağımlılık yönetimi için izole edilmiş sanal ortam (venv) üzerinde geliştirilmiştir.

### 🎥 Canlı Video Akışı
- Flask tabanlı MJPEG streaming
- Her İHA için ayrı video kanalı
- Anlık snapshot yakalama API'si

### 🌍 Dinamik Harita ve Raporlama
- Canlı GPS takibi (**Leaflet.js**)
- Tehdit unsurlarının koordinat bazlı loglanması
- Görev sonrası **PDF/Excel** formatında detaylı rapor dökümü (**QuestPDF**)

### 📊 İnteraktif Dashboard
- Telemetri widget'ları (Batarya, İrtifa, Hız, Pusula, Sinyal, Uçuş Süresi)
- Gerçek zamanlı grafikler (**Chart.js**)
- İHA bazlı filtreleme ve istatistikler
- Aranan plaka yönetim paneli

---

## 🛠️ Kullanılan Teknolojiler

### Backend (GCS Modülü)
| Teknoloji | Açıklama |
|-----------|----------|
| **C# (.NET 8.0)** | Ana programlama dili |
| **ASP.NET Core MVC** | Web framework |
| **SignalR** | Gerçek zamanlı WebSocket iletişimi |
| **Entity Framework Core** | Veritabanı ORM |
| **QuestPDF** | PDF rapor oluşturma |
| **SQLite** | Veritabanı |

### AI & Vision (İHA Modülü)
| Teknoloji | Açıklama |
|-----------|----------|
| **Python 3.10+** | Ana programlama dili |
| **OpenCV** | Görüntü işleme |
| **Ultralytics YOLOv8** | Nesne tespiti ve takibi |
| **EasyOCR** | Optik karakter tanıma (Plaka okuma) |
| **Flask** | Video streaming sunucusu |
| **PyCryptodome** | AES-256 şifreleme |

### Frontend
| Teknoloji | Açıklama |
|-----------|----------|
| **Bootstrap 5** | UI framework |
| **Leaflet.js** | İnteraktif harita |
| **Chart.js** | Grafikler |
| **SignalR Client** | Gerçek zamanlı veri alımı |

---

## 📁 Proje Yapısı

```
SVT-UAV-GCS/
│
├── 📂 UAV_Module_Python/           # İHA Tarafı (Python)
│   ├── uav_module.py               # Ana modül (YOLO + OCR + Flask)
│   ├── config.py                   # Yapılandırma ayarları
│   ├── security_utils.py           # AES-256 şifreleme
│   ├── gps_simulator.py            # GPS koordinat simülasyonu
│   ├── telemetry_simulator.py      # Telemetri veri simülasyonu
│   ├── requirements.txt            # Python bağımlılıkları
│   └── yolov8n.pt                  # YOLO model dosyası
│
├── 📂 GCS_Module/                   # Yer İstasyonu (C# ASP.NET Core)
│   ├── 📂 Controllers/             # MVC Controller'lar
│   ├── 📂 Models/                  # Veri modelleri
│   ├── 📂 Services/                # Arka plan servisleri
│   ├── 📂 Views/                   # Razor view'ları
│   ├── 📂 Hubs/                    # SignalR hub
│   └── Program.cs                  # Uygulama giriş noktası
│
└── README.md                        # Bu dosya
```

---

## 🔧 Kurulum ve Çalıştırma

Bu proje **Sanal Ortam (Virtual Environment)** kullanılarak geliştirilmiştir. Lütfen aşağıdaki adımları takip edin:

### 1️⃣ Repoyu Klonlayın

```bash
git clone https://github.com/umutugrasan/SVT-UAV-GCS.git
cd SVT-UAV-GCS
```

### 2️⃣ GCS Modülünü (C#) Başlatın

**Visual Studio 2022 ile:**
1. `GCS_Module` klasörünü Visual Studio 2022 ile açın
2. NuGet paketlerini restore edin
3. IIS Express veya Kestrel üzerinden çalıştırın

**Komut Satırı ile:**
```bash
cd GCS_Module

# Bağımlılıkları yükle
dotnet restore

# Veritabanı migration'larını uygula
dotnet ef database update

# Uygulamayı başlat
dotnet run
```

Tarayıcıda `http://localhost:5000` adresini açın.

**Varsayılan Giriş Bilgileri:**
- 👤 Kullanıcı Adı: `admin`
- 🔑 Şifre: `admin123`

### 3️⃣ İHA Modülünü (Python) Başlatın

Python bağımlılıklarının çakışmaması için sanal ortam kurmanız **zorunludur**.

```bash
cd UAV_Module_Python

# Sanal ortamı oluştur (Sadece ilk seferde)
python -m venv venv

# Sanal ortamı aktif et
# Windows:
.\venv\Scripts\activate
# Linux/Mac:
source venv/bin/activate

# Kütüphaneleri yükle
pip install -r requirements.txt

# Varsayılan İHA'yı başlat (UAV-001)
python uav_module.py
```

### 4️⃣ Çoklu İHA Çalıştırma (Opsiyonel)

Her İHA için ayrı terminal açın:

```bash
# Terminal 1 - UAV-001 (Port 5050)
python uav_module.py

# Terminal 2 - UAV-002 (Port 5051)
python uav_module.py --id UAV-002 --name "Bravo-2"

# Terminal 3 - UAV-003 (Port 5052)
python uav_module.py --id UAV-003 --name "Charlie-3" --video baska_video.mp4
```

---

## 📡 API Endpoint'leri

### Python Flask (İHA Modülü)
| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/video_feed` | GET | MJPEG canlı video akışı |
| `/snapshot` | GET | Anlık JPEG görüntü |
| `/update_wanted` | POST | Aranan plaka listesini güncelle |

### C# ASP.NET (Yer İstasyonu)
| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/` | GET | Ana dashboard |
| `/Account/Login` | POST | Kullanıcı girişi |
| `/Report/GeneratePdf` | POST | PDF rapor oluştur |
| `/Report/GenerateExcel` | POST | CSV rapor oluştur |
| `/targetHub` | WebSocket | SignalR bağlantısı |

---

## 🔮 Gelecek Geliştirmeler

- [ ] 3D Harita Entegrasyonu (CesiumJS)
- [ ] Drone Rota Planlama
- [ ] Çoklu Kamera Grid Görünümü
- [ ] Mobil Uygulama Desteği
- [ ] Docker Containerization
- [ ] Kubernetes Deployment

---

# 🇬🇧 ENGLISH

## 📋 About Project

**SVT GCS** is an advanced and secure UAV Ground Control Station built on **ASP.NET Core MVC** and **Python** architecture, with **Multi-UAV** support.

This project isolates image processing and AI algorithms on a **virtual environment**, preventing version conflicts and ensuring high stability.

**⚠️ This project is under active development. This is not the final version.**

---

## 🚀 Key Features

### 🚁 Multi-UAV Support
The system allows multiple UAVs to connect simultaneously, with separate data processing and management. Each UAV is tracked with its own ID and automatic port assignment:
- `UAV-001` → Port 5050
- `UAV-002` → Port 5051
- `UAV-003` → Port 5052

### 📡 Real-Time Telemetry
Altitude, speed, battery, position, and signal data from UAVs are transmitted to the interface without delay using **SignalR** (WebSocket) technology.

### 🧠 AI & Image Processing
- **Object Detection:** Real-time detection of vehicles, people, and other objects using **YOLOv8 (Ultralytics)**.
- **License Plate Recognition (LPR):** Vehicle plates are read using **EasyOCR** deep learning models and compared with wanted list.
- **Threat Detection:** Two-layer threat system with wanted plate detection and regional density analysis.

### 🔒 Secure Data Transmission
All data packets between UAV and Ground Station are protected with **AES-256 CBC** encryption and transmitted over TCP sockets.

### 💻 Virtual Environment Architecture
Python modules are developed on isolated virtual environment (venv) for stability and dependency management.

### 🎥 Live Video Streaming
- Flask-based MJPEG streaming
- Separate video channel for each UAV
- Instant snapshot capture API

### 🌍 Dynamic Map and Reporting
- Live GPS tracking (**Leaflet.js**)
- Coordinate-based logging of threat elements
- Post-mission detailed reports in **PDF/Excel** format (**QuestPDF**)

---

## 🔧 Installation and Running

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/umutugrasan/SVT-UAV-GCS.git
cd SVT-UAV-GCS
```

### 2️⃣ Start GCS Module (C#)

```bash
cd GCS_Module
dotnet restore
dotnet ef database update
dotnet run
```

Open `http://localhost:5000` in browser.

**Default Login:**
- 👤 Username: `admin`
- 🔑 Password: `admin123`

### 3️⃣ Start UAV Module (Python)

```bash
cd UAV_Module_Python

# Create virtual environment (first time only)
python -m venv venv

# Activate virtual environment
# Windows:
.\venv\Scripts\activate
# Linux/Mac:
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Start default UAV
python uav_module.py
```

### 4️⃣ Running Multiple UAVs

```bash
# Terminal 1 - UAV-001 (Port 5050)
python uav_module.py

# Terminal 2 - UAV-002 (Port 5051)
python uav_module.py --id UAV-002 --name "Bravo-2"

# Terminal 3 - UAV-003 (Port 5052)
python uav_module.py --id UAV-003 --name "Charlie-3"
```

---

## 🔮 Future Developments

- [ ] 3D Map Integration (CesiumJS)
- [ ] Drone Route Planning
- [ ] Multi-Camera Grid View
- [ ] Mobile App Support
- [ ] Docker Containerization
- [ ] Kubernetes Deployment

---

## 👨‍💻 Geliştirici / Developer

<div align="center">

**Umut Uğraşan**

[![GitHub](https://img.shields.io/badge/GitHub-umutugrasan-181717?style=for-the-badge&logo=github)](https://github.com/umutugrasan)

</div>

---

<div align="center">

**⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın! ⭐**

**⭐ If you liked this project, don't forget to give it a star! ⭐**

</div>

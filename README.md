# SVT-UAV-GCS (Secure Vision Telemetry)

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Status](https://img.shields.io/badge/status-Active-success.svg)
![Tech](https://img.shields.io/badge/tech-C%23%20%7C%20Python%20%7C%20AI-purple.svg)

**SVT GCS**; **ASP.NET Core MVC** ve **Python** mimarisi üzerine inşa edilmiş, **Çoklu İHA (Multi-UAV)** desteğine sahip, gelişmiş ve güvenli bir İHA Yer Kontrol İstasyonudur.

Bu proje, görüntü işleme ve yapay zeka algoritmalarını **sanal ortam (virtual environment)** üzerinde izole ederek, sürüm uyuşmazlıklarının önüne geçmiş ve yüksek kararlılıkla çalışacak şekilde tasarlanmıştır.

---

## 🚀 Öne Çıkan Özellikler

* **🚁 Çoklu İHA Desteği (Multi-UAV Support):** Sistem, aynı anda birden fazla İHA'nın bağlanmasına, verilerinin ayrı ayrı işlenmesine ve yönetilmesine olanak tanır. Her İHA kendi kimliği (ID) ile takip edilir.
* **📡 Gerçek Zamanlı Telemetri:** İHA'lardan gelen irtifa, hız, batarya, konum ve sinyal verileri **SignalR** (WebSocket) teknolojisi ile gecikmesiz olarak arayüze aktarılır.
* **🧠 Yapay Zeka & Görüntü İşleme:**
    * **Nesne Tespiti:** **YOLO (You Only Look Once)** algoritması ile araç, insan ve diğer nesneler anlık tespit edilir.
    * **Plaka Tanıma (LPR):** **EasyOCR** derin öğrenme modelleri kullanılarak araç plakaları okunur ve arananlar listesiyle karşılaştırılır.
* **🔒 Güvenli Veri İletimi:** İHA ve Yer İstasyonu arasındaki tüm veri paketleri, özel şifreleme algoritmaları ile korunarak TCP soketleri üzerinden iletilir.
* **💻 Sanal Ortam Mimarisi:** Python modülleri, kararlılık ve bağımlılık yönetimi için izole edilmiş sanal ortam (venv/conda) üzerinde geliştirilmiştir.
* **🌍 Dinamik Harita ve Raporlama:**
    * Canlı GPS takibi (**Leaflet.js**).
    * Tehdit unsurlarının koordinat bazlı loglanması.
    * Görev sonrası PDF/Excel formatında detaylı rapor dökümü.

---

## 🛠️ Kullanılan Teknolojiler

### Backend (GCS Modülü)
* **Dil:** C# (.NET 8.0)
* **Çatı:** ASP.NET Core MVC
* **İletişim:** SignalR (Real-time), TCP Sockets
* **Veritabanı:** Entity Framework Core

### AI & Vision (İHA Modülü)
* **Dil:** Python 3.x
* **Kütüphaneler:** OpenCV, Ultralytics YOLOv8, EasyOCR, PyTorch
* **Ortam:** Virtual Environment (venv)

---

## 🔧 Kurulum ve Çalıştırma

Bu proje **Sanal Ortam (Virtual Environment)** kullanılarak geliştirilmiştir. Lütfen aşağıdaki adımları takip edin:

1.  **Repoyu Klonlayın:**
    ```bash
    git clone [https://github.com/umutugrasan/SVT-UAV-GCS.git](https://github.com/umutugrasan/SVT-UAV-GCS.git)
    ```

2.  **GCS Modülünü (C#) Başlatın:**
    `GCS_Module` klasörünü Visual Studio 2022 ile açın ve IIS Express üzerinden çalıştırın.

3.  **İHA Modülünü (Python) Başlatın:**
    Python bağımlılıklarının çakışmaması için sanal ortam kurmanız önerilir.

    ```bash
    cd UAV_Module
    
    # Sanal ortamı oluştur (Sadece ilk seferde)
    python -m venv venv

    # Sanal ortamı aktif et (Windows)
    .\venv\Scripts\activate

    # Kütüphaneleri yükle
    pip install -r requirements.txt

    # Modülü başlat
    python main.py
    ```

---

## 👨‍💻 Geliştirici

**Umut Uğraşan** tarafından geliştirilmiştir.




---

# SVT-UAV-GCS (Secure Vision Telemetry) - English Description

**SVT GCS** is an advanced, secure UAV Ground Control Station built on **ASP.NET Core MVC** and **Python**, featuring robust **Multi-UAV support**.

The project was developed using a **virtual environment** architecture to isolate image processing and AI algorithms, ensuring high stability and preventing dependency conflicts.

---

## 🚀 Key Features

* **🚁 Multi-UAV Support:** The system supports simultaneous connections from multiple UAVs. Each drone is tracked, managed, and processed independently with unique IDs.
* **📡 Real-Time Telemetry:** Live data such as altitude, speed, battery, and GPS location is transmitted instantly to the dashboard via **SignalR** (WebSocket).
* **🧠 AI & Computer Vision:**
    * **Object Detection:** Real-time detection of vehicles and people using **YOLO**.
    * **License Plate Recognition (LPR):** Automatic reading of license plates using **EasyOCR** (Deep Learning based) and matching against wanted lists.
* **🔒 Secure Communication:** All data transmission between the UAV and GCS is encrypted and transmitted over TCP sockets.
* **💻 Virtual Environment Architecture:** Python modules are developed within an isolated virtual environment (venv) to ensure dependency stability.
* **🌍 Dynamic Dashboard:**
    * Live GPS tracking on **Leaflet.js** maps.
    * Threat logging with precise coordinates.
    * Post-mission reporting in PDF/Excel formats.

---

## 🔧 Installation & Setup

This project relies on a **Virtual Environment** for the Python module.

1.  **Clone the repo:**
    ```bash
    git clone [https://github.com/umutugrasan/SVT-UAV-GCS.git](https://github.com/umutugrasan/SVT-UAV-GCS.git)
    ```

2.  **Run GCS Module (C#):**
    Open `GCS_Module` in Visual Studio and run via IIS Express.

3.  **Run UAV Module (Python):**
    ```bash
    cd UAV_Module
    
    # Create virtual environment (First time only)
    python -m venv venv

    # Activate virtual environment (Windows)
    .\venv\Scripts\activate

    # Install dependencies
    pip install -r requirements.txt

    # Run the module
    python main.py
    ```

# SVT Projesi — Çalıştırma ve Demo Rehberi (Tam / Offline)

Bu dosya, projeyi **sıfırdan çalıştırmak** için gereken her şeyi içerir: kurulum,
çalıştırma, **çoklu İHA**, arayüz kullanımı ve sorun giderme. Sistem **internetsiz**
(tam offline) çalışacak şekilde hazırlanmıştır — harita dahil.

> Bu bilgisayarda gerekli her şey zaten kurulu. Aşağıdaki "İlk kurulum" bölümü sadece
> **başka bir bilgisayara** taşırsan gereklidir.

---

## 0. Sistem nedir? (1 paragraf)

İki modülden oluşur:
- **UAV Modülü (Python):** Videoyu işler, YOLOv8 ile araç/insan tespiti + EasyOCR ile plaka
  okuma yapar, GPS/telemetri simüle eder, veriyi **AES-256 ile şifreleyip** TCP üzerinden
  GCS'e gönderir. Ayrıca canlı görüntüyü bir web portundan yayınlar.
- **GCS Modülü (C# / ASP.NET Core):** Şifreli veriyi çözer, veritabanına yazar ve web
  arayüzünde canlı gösterir: harita, grafikler, telemetri, tehdit alarmı, PDF/Excel rapor.

---

## 1. Gereksinimler (bu makinede kurulu ✔)

| Bileşen | Ne için | Durum |
|---|---|---|
| .NET 8 SDK | GCS (C#) arayüzü | ✔ |
| SQL Server LocalDB | Veritabanı | ✔ |
| Anaconda + `svt_env` ortamı (Python 3.10) | UAV modülü | ✔ |
| YOLO modeli (`yolov8s.pt` / `yolov8n.pt`) | Araç tespiti | ✔ proje içinde |
| EasyOCR modelleri | Plaka okuma | ✔ önbellekte (`.EasyOCR`) |
| Web kütüphaneleri (Leaflet, Chart.js, SignalR, Bootstrap) | Arayüz | ✔ projeye gömülü (offline) |
| Harita altlığı | Sokak görüntüsü | 🌐 canlı OpenStreetMap (internet ister) |

---

## 2. İlk kurulum (SADECE yeni bir bilgisayarda gerekli)

Bu makinede atla. Yeni makinede:

```powershell
# 1) Python ortamı
conda create -n svt_env python=3.10 -y
conda activate svt_env
cd UAV_Module_Python
pip install -r requirements.txt

# 2) EasyOCR modellerini indir (İNTERNET GEREKİR — bir kez):
#    Modülü internete bağlıyken bir kez çalıştırınca modeller otomatik iner.

# 3) Veritabanı (GCS klasöründe)
cd ..\GCS_Module_NET\GCS_Module
dotnet ef database update
#    (dotnet-ef yoksa:  dotnet tool install --global dotnet-ef)
```

> Not: Uygulama ilk çalıştığında admin kullanıcısını otomatik oluşturur
> (kullanıcı: `admin`, şifre: `admin123`).

---

## 3. Çalıştırma — Tek İHA (temel demo)

İki ayrı terminal kullanılır.

### Terminal 1 — GCS (Web Arayüzü)
```powershell
cd C:\Users\UMUT\Desktop\SVT_PROJECT\GCS_Module_NET\GCS_Module
dotnet run
```
- Tarayıcı otomatik açılır → **http://localhost:5090**
- Giriş: **admin** / **admin123**

### Terminal 2 — UAV Modülü (Python + AI)
```powershell
conda activate svt_env
cd C:\Users\UMUT\Desktop\SVT_PROJECT\UAV_Module_Python
python uav_module.py --id UAV-001 --name Alpha-1 --video test_video.mp4
```
- "OCR Motoru Yükleniyor" birkaç saniye sürer (normal).
- Bir OpenCV penceresi açılır (tespit görüntüsü). Kapatmak: pencere seçiliyken `q`.
- Arayüzde video, telemetri, harita ve grafikler birkaç saniye içinde gelir.

> **conda tanınmıyor** hatası alırsan bkz. Bölüm 7. Alternatif garanti yöntem:
> ```powershell
> C:\Users\UMUT\anaconda3\envs\svt_env\python.exe uav_module.py --id UAV-001 --name Alpha-1 --video test_video.mp4
> ```

Durdurmak: her terminalde `Ctrl+C`.

---

## 4. Çoklu İHA (farklı videolarla) 🚁🚁

Her İHA **ayrı bir terminal / ayrı bir process**'tir. Portlar ID'den otomatik hesaplanır:

| İHA | Video portu | Not |
|---|---|---|
| UAV-001 | 8090 | ID numarasından: `8090 + (numara - 1)` |
| UAV-002 | 8091 | |
| UAV-003 | 8092 | |

> Video portları (8090+) GCS'in veri portundan (5050) **ayrıdır** — çakışma olmaz.

### Adımlar
1. **İkinci bir video** ekle: bir `.mp4` dosyasını `UAV_Module_Python` klasörüne koy
   (ör. `test_video2.mp4`). Video bulma: bkz. Bölüm 8.
2. **Terminal 1:** GCS zaten çalışıyorsa yeter (tek GCS tüm İHA'lara hizmet verir).
3. **Terminal 2 — 1. İHA:**
   ```powershell
   conda activate svt_env
   cd C:\Users\UMUT\Desktop\SVT_PROJECT\UAV_Module_Python
   python uav_module.py --id UAV-001 --name Alpha-1 --video test_video.mp4
   ```
4. **Terminal 3 — 2. İHA (farklı id + farklı video):**
   ```powershell
   conda activate svt_env
   cd C:\Users\UMUT\Desktop\SVT_PROJECT\UAV_Module_Python
   python uav_module.py --id UAV-002 --name Bravo-2 --video test_video2.mp4
   ```
5. Arayüzde üstte **iki İHA butonu** çıkar (Alpha-1 / Bravo-2). Birine tıkla → video ve
   veriler o İHA'ya geçer. **"Tümü"** → hepsi birlikte.

> Elinde tek video varsa, aynı videoyu farklı `--id` ile ikinci İHA olarak da başlatabilirsin
> (görüntü aynı olur ama iki ayrı kanal/port gibi davranır).

### Çoklu İHA'da performans
CPU'da aynı anda birden çok AI işlemi yükü artırır. Daha akıcı olması için
[UAV_Module_Python/config.py](UAV_Module_Python/config.py) içinde tek satır değiştir:
```python
MODEL_PATH = 'yolov8n.pt'   # 'yolov8s.pt' yerine — nano model ~2-3x daha hızlı
```
`yolov8n.pt` zaten klasörde var.

---

## 5. Arayüzü kullanma

- **Aktif UAV'lar (üst):** İHA seç → o İHA'nın canlı videosu + verileri. İlk İHA bağlanınca
  video **otomatik** açılır.
- **Aranan Plakalar:** kutuya plaka yaz → **Ekle**. Liste tüm bağlı İHA'lara gönderilir.
- **Tehdit tespiti:** aranan bir plaka görülünce kırmızı kutu + **Tehdit Sayısı artar** +
  tehdit günlüğü + alarm penceresi. (Sağlam tetikleme için bkz. Bölüm 6.)
- **Telemetri panelleri:** batarya, irtifa, hız, pusula, sinyal, uçuş süresi.
- **Harita:** araçların canlı GPS konumu (offline harita üzerinde).
- **Grafikler:** hedef tipi dağılımı + zaman serisi.
- **Raporlar:** **PDF Rapor** / **Excel Rapor** butonları ile görev raporu indir.

---

## 6. Tehdit (aranan plaka) demosunu garantiye alma 🎯

OCR gerçek bir videodan plakayı **birebir temiz okuyamaz** (harf/rakam eksik/yanlış olabilir).
Sistem bu yüzden **esnek eşleştirme** yapar (kısmi ya da rakam çekirdeği eşleşse de yakalar).
Kusursuz demo için:

1. Video çalışırken açılan pencerede araçların üstünde okunan plakayı gör (`[34ABC12]` gibi),
   veya Python terminalinde / `uav_module.log` içinde şu satırları izle:
   ```
   🔤 OCR okudu (ID 5): 34CSH571
   ```
2. Orada **gerçekten okunan** metni (ya da sadece rakamlarını, ör. `571`) arayüzdeki
   **"Aranan Plakalar"** kutusuna ekle.
3. Aynı araç tekrar geçince tehdit tetiklenir.

> Log'da hiç `🔤 OCR okudu` çıkmıyorsa, o videoda plakalar çok küçük/bulanıktır → plakası
> daha net görünen bir video kullan.

---

## 7. Sorun giderme

**`conda: The term 'conda' is not recognized...`**
Conda PowerShell'e tanıtılmamış. Bir kerelik:
```powershell
C:\Users\UMUT\anaconda3\Scripts\conda.exe init powershell
```
Sonra terminali **kapat-aç**. Gerekirse: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.
(Ya da hiç uğraşmadan Bölüm 3'teki tam-yol yöntemini kullan.)

**Panelde video gelmiyor / "SİNYAL ARANIYOR" yazıyor**
Çoğunlukla GCS eski kodla çalışıyordur. Şunu yap:
1. GCS terminalinde `Ctrl+C` → tekrar `dotnet run`
2. Python İHA'larını da yeniden başlat
3. Tarayıcıda **`Ctrl+F5`** (eski JavaScript önbelleğini at)

**Kod değişikliği sonrası hiçbir şey değişmiyor**
Değişiklikler C# tarafındaysa GCS'i yeniden başlat; JavaScript/arayüzse tarayıcıda `Ctrl+F5`.

**Tehdit Sayısı hep 0**
Bkz. Bölüm 6 — OCR'ın gerçekten okuduğu plakayı ekle.

**Port zaten kullanımda (Address already in use)**
Eski bir process açık kalmıştır. Kapat:
```powershell
Get-Process python,dotnet | Stop-Process -Force
```
sonra yeniden başlat.

---

## 8. Daha fazla video nereden bulunur (ücretsiz)

- **Pexels** (pexels.com/videos), **Pixabay** (pixabay.com/videos), **Mixkit**
  (mixkit.co/free-stock-video), **Videvo**, **Coverr** — telifsiz, direkt mp4 indir.
- Arama terimleri (İngilizce): `traffic`, `highway traffic`, `dashcam`, `drone traffic`,
  `license plate`, `city street cars`.
- Öneri: **720p**, **20–60 sn**, araçların net göründüğü sahne. İndirdiğini
  `UAV_Module_Python` klasörüne koy, `--video dosya_adi.mp4` ile çağır.

---

## 9. Offline durumu (özet)

İnternetsiz **çalışan** (tam offline): web arayüzü, canlı video, AI tespiti, plaka okuma,
telemetri, grafikler, tehdit alarmı, şifreleme, veritabanı, PDF/Excel rapor.

**Tek istisna — harita altlığı:** Harita sokak görüntüsü canlı OpenStreetMap'ten gelir, yani
**internet ister**. İnternetsiz ortamda harita arka planı boş/gri kalır, ama araç işaretçileri
(renkli daireler) yine doğru GPS konumunda görünür ve çalışır. Diğer her şey internetsiz tamdır.

> Yeni bir bilgisayarda offline göstereceksen: .NET 8 SDK + LocalDB + `svt_env` kurulu olmalı,
> ve EasyOCR modelleri bir kez (internetliyken) indirilmiş olmalı (Bölüm 2).

---

## 10. Hızlı komut özeti

```powershell
# GCS (Terminal 1)
cd C:\Users\UMUT\Desktop\SVT_PROJECT\GCS_Module_NET\GCS_Module
dotnet run
#  → http://localhost:5090   (admin / admin123)

# UAV-001 (Terminal 2)
conda activate svt_env
cd C:\Users\UMUT\Desktop\SVT_PROJECT\UAV_Module_Python
python uav_module.py --id UAV-001 --name Alpha-1 --video test_video.mp4

# UAV-002 (Terminal 3) — çoklu İHA
conda activate svt_env
cd C:\Users\UMUT\Desktop\SVT_PROJECT\UAV_Module_Python
python uav_module.py --id UAV-002 --name Bravo-2 --video test_video2.mp4
```

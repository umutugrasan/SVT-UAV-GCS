import easyocr
import threading
import queue
from flask import Flask, Response, request
import cv2
import os
import socket
import time
import json
import logging
import argparse
from datetime import datetime
from ultralytics import YOLO
from security_utils import encrypt_target_data
from gps_simulator import GPSSimulator
from telemetry_simulator import TelemetrySimulator
from config import Config

# Global Arananlar Listesi (Varsayılan boş veya test amaçlı dolu olabilir)
WANTED_PLATES = []
plate_lock = threading.Lock() # Çakışmayı önlemek için kilit

# --- ASENKRON OCR (PLAKA OKUMA) ---
# OCR ağır olduğu için ana görüntü döngüsünü bloklamasın diye ayrı bir iş parçacığında
# çalışır. Ana döngü sadece kırpılmış aracı kuyruğa atar ve en son bilinen plaka sonucunu
# önbellekten okur. Böylece video akışı asla donmaz.
ocr_queue = queue.Queue(maxsize=8)     # işlenmeyi bekleyen araç kırpıntıları
plate_cache = {}                       # track_id -> {"plate": str, "ts": float}
plate_cache_lock = threading.Lock()


def _normalize_plate(s):
    """Plakayı karşılaştırma için sadeleştir: sadece harf+rakam, büyük harf."""
    return ''.join(ch for ch in s.upper() if ch.isalnum())


def is_plate_wanted(detected_text):
    """
    OCR gerçek videoda plakayı asla birebir temiz okuyamaz (harf/rakam eksik/yanlış olur).
    Bu yüzden ESNEK eşleştirme yapılır: aranan plaka okunan metnin içinde geçiyorsa
    (veya tersi), ya da rakam çekirdekleri aynıysa TEHDİT sayılır.
    """
    ct = _normalize_plate(detected_text)
    if len(ct) < 4:
        return False
    with plate_lock:
        wanted = list(WANTED_PLATES)
    ct_digits = ''.join(c for c in ct if c.isdigit())
    for w in wanted:
        wn = _normalize_plate(w)
        if len(wn) < 4:
            continue
        # 1) tam eşleşme veya biri diğerini içeriyor
        if wn == ct or wn in ct or ct in wn:
            return True
        # 2) rakam çekirdeği eşleşiyor (örn. aranan "34CSH571" ↔ okunan "CSH 571 X")
        wn_digits = ''.join(c for c in wn if c.isdigit())
        if len(wn_digits) >= 3 and wn_digits == ct_digits:
            return True
    return False


# Logging sistemi
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - [%(levelname)s] - %(message)s',
    handlers=[
        logging.FileHandler('uav_module.log'),
        logging.StreamHandler()
    ]
)

# --- FLASK İÇİN GEREKLİ ---
app = Flask(__name__)
outputFrame = None
lock = threading.Lock()

# Global telemetry simulator
telemetry_sim = TelemetrySimulator()

# --- CANLI YAYIN STREAM KODLARI ---
def generate():
    global outputFrame, lock
    while True:
        with lock:
            if outputFrame is None:
                wait = True
            else:
                wait = False
                (flag, encodedImage) = cv2.imencode(".jpg", outputFrame)

        if wait:
            time.sleep(0.05)
            continue

        if not flag:
            continue

        yield(b'--frame\r\n' b'Content-Type: image/jpeg\r\n\r\n' + bytearray(encodedImage) + b'\r\n')

@app.route("/video_feed")
def video_feed():
    return Response(generate(), mimetype = "multipart/x-mixed-replace; boundary=frame")

@app.route("/snapshot")
def snapshot():
    global outputFrame, lock
    with lock:
        if outputFrame is None:
            return "", 204
        (flag, encodedImage) = cv2.imencode(".jpg", outputFrame)
        if not flag: return "", 204
        
    return Response(encodedImage.tobytes(), mimetype="image/jpeg")

# --- YENİ EKLENEN: LİSTE GÜNCELLEME KAPISI ---
@app.route("/update_wanted", methods=['POST', 'OPTIONS'])
def update_wanted():
    global WANTED_PLATES
    
    # Tarayıcı ön kontrolü (CORS) için izin ver
    if request.method == 'OPTIONS':
        resp = Response("")
        resp.headers['Access-Control-Allow-Origin'] = "*"
        resp.headers['Access-Control-Allow-Methods'] = "POST"
        resp.headers['Access-Control-Allow-Headers'] = "Content-Type"
        return resp

    try:
        # Gelen veriyi al
        data = request.json
        new_plates = data.get('plates', [])
        
        # Listeyi güncelle
        with plate_lock:
            WANTED_PLATES = new_plates
            
        logging.info(f"📋 ARANANLAR LİSTESİ GÜNCELLENDİ: {WANTED_PLATES}")
        
        # Başarılı cevabı dön
        resp = Response(json.dumps({"status": "success", "count": len(WANTED_PLATES)}), mimetype="application/json")
        resp.headers['Access-Control-Allow-Origin'] = "*"
        return resp
        
    except Exception as e:
        return Response(json.dumps({"status": "error", "message": str(e)}), status=500, mimetype="application/json")
    
    
def start_flask(port_num):
    app.run(host="0.0.0.0", port=port_num, debug=False, use_reloader=False, threaded=True)
    
def ocr_worker(reader):
    """
    Arka planda çalışır: kuyruktan (track_id, araç_görüntüsü) alır, plakayı okur ve
    sonucu plate_cache'e yazar. Ana döngü bu iş parçacığını beklemez.
    """
    while True:
        try:
            track_id, roi = ocr_queue.get()
        except Exception:
            continue
        try:
            if roi is not None and roi.size > 0:
                detections = reader.readtext(roi, detail=0)
                for text in detections:
                    clean_text = text.replace(" ", "").upper()
                    # Basit plaka kontrolü (6-9 karakter ve en az bir rakam)
                    if 6 <= len(clean_text) <= 9 and any(c.isdigit() for c in clean_text):
                        with plate_cache_lock:
                            plate_cache[track_id] = {"plate": text.upper(), "ts": time.time()}
                        # OCR'ın ne okuduğunu görebilmen için logla (aranan listeye ekleyeceğin metin)
                        logging.info(f"🔤 OCR okudu (ID {track_id}): {text.upper()}")
                        break
        except Exception:
            pass  # OCR hatası olursa iş parçacığı çökmesin
        finally:
            ocr_queue.task_done()


def connect_to_gcs(uav_name):
    """
    GCS'e (C# sunucusu) tek bir bağlantı denemesi yapar.
    Başarılıysa bağlı socket, başarısızsa None döner.
    Bağlantı denemesine timeout konur ki sunucu kapalıyken video akışı kilitlenmesin.
    """
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(Config.CONNECTION_TIMEOUT)  # bağlanma denemesi kısa sürsün
        s.connect((Config.SERVER_IP, Config.SERVER_PORT))
        s.settimeout(None)  # gönderimler için normal (bloklayan) moda dön
        logging.info(f"🔗 [{uav_name}] GCS BAĞLANTISI AKTİF: {Config.SERVER_IP}:{Config.SERVER_PORT}")
        return s
    except Exception:
        return None


def send_batch_data(client_socket, batch_data, uav_name):
    max_retries = 3
    for attempt in range(max_retries):
        try:
            json_data = json.dumps({"targets": batch_data})
            encrypted_packet = encrypt_target_data(json_data)
            message = (encrypted_packet + "\n").encode('utf-8')
            client_socket.send(message)
            logging.info(f"✅ [{uav_name}] {len(batch_data)} hedef gönderildi | 🔋 {telemetry_sim.battery:.1f}%")
            return True
        except Exception as e:
            logging.warning(f"⚠️ [{uav_name}] Gönderim hatası (Deneme {attempt+1}/{max_retries}): {e}")
            time.sleep(0.5)
    return False

# ==========================================
# 🧠 GÜNCELLENMİŞ ANA GÖRÜŞ FONKSİYONU 🧠
# ==========================================
def start_uav_vision(video_name, uav_id, uav_name):
    global outputFrame, lock 
    
    current_dir = os.path.dirname(os.path.abspath(__file__))
    video_path = os.path.join(current_dir, video_name)
    
    if not os.path.exists(video_path):
        logging.error(f"❌ HATA: Video dosyası bulunamadı! Aranan yol: {video_path}")
        return

    window_name = f"UAV Vision - {uav_name} ({uav_id})"
    
    # --- SOCKET BAĞLANTISI (otomatik yeniden bağlanmalı) ---
    client_socket = connect_to_gcs(uav_name)
    connected = client_socket is not None
    if not connected:
        logging.warning(f"⚠️ [{uav_name}] GCS Offline. Arka planda otomatik bağlanmaya çalışılacak...")
    last_reconnect_attempt = time.time()
    RECONNECT_INTERVAL = 3  # saniyede bir yeniden bağlanma denemesi
    
    # --- OCR MOTORUNU BAŞLAT (YENİ) ---
    print("👀 OCR Motoru Yükleniyor... (Lütfen bekleyin)")
    # Eğer NVIDIA Ekran Kartın Varsa gpu=True yap, yoksa False kalsın
    reader = easyocr.Reader(['en'], gpu=False)
    print("✅ OCR Motoru Hazır!")

    # OCR'ı arka planda çalıştıran iş parçacığını başlat (video donmasını engeller)
    ocr_thread = threading.Thread(target=ocr_worker, args=(reader,), daemon=True)
    ocr_thread.start()
    last_ocr_submit = {}          # track_id -> son OCR'a gönderilen frame numarası
    OCR_SUBMIT_INTERVAL = 15      # her araç için kaç karede bir OCR denensin
    

    # --- MODEL YÜKLEME ---
    model = YOLO(Config.MODEL_PATH)
    model.to('cpu')
    cap = cv2.VideoCapture(video_path)
    
    frame_width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    frame_height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    gps_sim = GPSSimulator(Config.BASE_LATITUDE, Config.BASE_LONGITUDE)
    
    detection_buffer = []
    last_send_time = time.time()
    frame_counter = 0

    logging.info(f"🚀 [{uav_name}] SVT Operasyon Başladı")

    try:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret: 
                logging.info("Video bitti, başa sarılıyor...")
                cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
                continue
            
            frame_counter += 1
            
            # Telemetri güncelle
            telemetry_data = telemetry_sim.get_data()
            if telemetry_data["battery"] < 20:
                logging.warning(f"⚠️ [{uav_name}] DÜŞÜK BATARYA: {telemetry_data['battery']:.1f}%")
            
            # --- NESNE TAKİBİ ---
            # CPU'da akıcılık için imgsz 480 (640 yerine); araç tespiti için yeterli.
            results = model.track(frame, persist=True, conf=Config.CONFIDENCE_THRESHOLD, imgsz=480, verbose=False)
            
            current_frame_targets = 0
            
            for r in results:
                if r.boxes.id is not None:
                    ids = r.boxes.id.int().tolist()
                    for box, obj_id in zip(r.boxes, ids):
                        cls = int(box.cls[0])
                        name = model.names[cls]
                        conf = float(box.conf[0])
                        b = box.xyxy[0].tolist() # [x1, y1, x2, y2]
                        
                        # Koordinatları tam sayıya çevir
                        x1, y1, x2, y2 = int(b[0]), int(b[1]), int(b[2]), int(b[3])

                        detected_plate = "BELIRSIZ"
                        is_wanted_plate = False

                        # SADECE ARAÇLARDA PLAKA OKU
                        if name in ['car', 'truck', 'bus']:
                            current_frame_targets += 1

                            # --- ASENKRON OCR: aracı kuyruğa at (ana döngü BEKLEMEZ) ---
                            last = last_ocr_submit.get(obj_id, -OCR_SUBMIT_INTERVAL)
                            if frame_counter - last >= OCR_SUBMIT_INTERVAL:
                                h_img, w_img, _ = frame.shape
                                y1_c, y2_c = max(0, y1), min(h_img, y2)
                                x1_c, x2_c = max(0, x1), min(w_img, x2)
                                car_roi = frame[y1_c:y2_c, x1_c:x2_c]
                                if car_roi.size > 0:
                                    try:
                                        ocr_queue.put_nowait((obj_id, car_roi.copy()))
                                        last_ocr_submit[obj_id] = frame_counter
                                    except queue.Full:
                                        pass  # kuyruk doluysa bu kareyi atla, video akmaya devam etsin

                            # En son bilinen plaka sonucunu önbellekten oku
                            with plate_cache_lock:
                                cached = plate_cache.get(obj_id)
                            if cached:
                                detected_plate = cached["plate"]
                                # Aranan mı? (esnek eşleşme + güncel liste - sonradan eklesen bile yanar)
                                is_wanted_plate = is_plate_wanted(detected_plate)
                                if is_wanted_plate:
                                    print(f"🚨 DİKKAT! ARANAN PLAKA: {detected_plate}")

                        # GPS Hesapla
                        cx, cy = (x1 + x2) // 2, (y1 + y2) // 2
                        lat, lon = gps_sim.get_coordinates(cx, cy, frame_width, frame_height)

                        # Veri Paketini Oluştur
                        detection_data = {
                            "uavId": uav_id, 
                            "uavName": uav_name, 
                            "trackId": obj_id,
                            "type": name, 
                            "pixelCoords": f"[{cx}, {cy}]", 
                            "gpsCoords": f"[{lat:.6f}, {lon:.6f}]",
                            "confidence": round(conf, 2), 
                            "telemetry": telemetry_data,
                            "plate": detected_plate,     # <--- YENİ: PLAKA
                            "isThreat": is_wanted_plate, # <--- YENİ: ARANAN PLAKA İSE TEHDİT
                            "timestamp": datetime.now().isoformat()
                        }
                        
                        # Veriyi sadece OCR yapıldığında veya tehdit varsa gönderelim (Performans için)
                        if frame_counter % 5 == 0 or is_wanted_plate:
                             detection_buffer.append(detection_data)

                        # --- GÖRSELLEŞTİRME ---
                        # Renk Ayarı: Aranan plaka ise KIRMIZI, değilse YEŞİL
                        if is_wanted_plate:
                            box_color = (0, 0, 255) # Kırmızı
                            label_text = f"ID:{obj_id} {name} | ARANIYOR: {detected_plate}"
                        else:
                            box_color = (0, 255, 0) # Yeşil
                            label_text = f"ID:{obj_id} {name}"
                            if detected_plate != "BELIRSIZ":
                                label_text += f" [{detected_plate}]"

                        cv2.rectangle(frame, (x1, y1), (x2, y2), box_color, 2)
                        cv2.putText(frame, label_text, (x1, y1-10), 
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, box_color, 2)
                        
            # Görüntüyü Web Yayını İçin Kopyala
            with lock:
                outputFrame = frame.copy()

            # --- OTOMATİK YENİDEN BAĞLANMA ---
            # GCS kapalı/koptuysa program çökmez; arka planda periyodik olarak yeniden dener.
            if not connected and (time.time() - last_reconnect_attempt >= RECONNECT_INTERVAL):
                last_reconnect_attempt = time.time()
                client_socket = connect_to_gcs(uav_name)
                if client_socket is not None:
                    connected = True
                    logging.info(f"🔄 [{uav_name}] GCS'e yeniden bağlanıldı.")
                    detection_buffer.clear()  # bağlantı yokken biriken eski veriyi at

            # Bağlantı yokken tampon sonsuza kadar büyümesin (bellek koruması)
            if not connected and len(detection_buffer) > Config.MAX_BATCH_SIZE:
                detection_buffer = detection_buffer[-Config.MAX_BATCH_SIZE:]

            # Plaka önbelleği ve OCR takibini periyodik temizle (bellek koruması)
            if frame_counter % 300 == 0:
                now_ts = time.time()
                with plate_cache_lock:
                    for tid in [k for k, v in plate_cache.items() if now_ts - v["ts"] > 60]:
                        plate_cache.pop(tid, None)
                for tid in [k for k, v in last_ocr_submit.items() if frame_counter - v > 300]:
                    last_ocr_submit.pop(tid, None)

            # --- BATCH GÖNDERİM ---
            if (time.time() - last_send_time >= Config.BATCH_INTERVAL) and detection_buffer and connected:
                if send_batch_data(client_socket, detection_buffer, uav_name):
                    detection_buffer.clear()
                    last_send_time = time.time()
                else:
                    # Gönderim başarısız: bağlantıyı kapalı işaretle, çökme YOK, sonra tekrar bağlanılır.
                    logging.error(f"❌ [{uav_name}] Sunucu bağlantısı koptu! Otomatik yeniden bağlanma bekleniyor...")
                    connected = False
                    try:
                        client_socket.close()
                    except Exception:
                        pass
                    client_socket = None
                    last_reconnect_attempt = time.time()

            # --- EKRAN BİLGİ PANELİ (OSD) ---
            # Üst panel arkaplanı
            cv2.rectangle(frame, (0, 0), (frame_width, 90), (0, 0, 0), -1)
            
            # Yazılar...
            cv2.putText(frame, f"[{uav_id}] {uav_name}", (10, 25), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
            cv2.putText(frame, f"BAT: {telemetry_data['battery']:.1f}%", (10, 55), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
            cv2.putText(frame, f"OCR (ASENKRON) KUYRUK: {ocr_queue.qsize()}", (350, 55), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 200), 2)
            
            cv2.namedWindow(window_name, cv2.WINDOW_NORMAL)
            cv2.resizeWindow(window_name, 1280, 720)
            cv2.imshow(window_name, frame)
            
            if cv2.waitKey(1) & 0xFF == ord('q'):
               break

    finally:
        if client_socket is not None:
            try:
                client_socket.close()
            except Exception:
                pass
        cap.release()
        cv2.destroyAllWindows()
        logging.info(f"🛑 [{uav_name}] SVT Operasyon Sonlandırıldı")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="SVT UAV Module")
    parser.add_argument("--id", default=Config.UAV_ID, help="UAV benzersiz ID'si")
    parser.add_argument("--name", default=Config.UAV_NAME, help="UAV görünen adı")
    parser.add_argument("--video", default="test_video.mp4", help="Video dosyası")
    
    args = parser.parse_args()
    
    # NOT: Video (Flask) portu, GCS'in TCP veri portundan (5050) AYRI tutulur; yoksa
    # UAV-001 için 5050 çakışır ve video sunucusu başlayamaz. Bu yüzden taban 8090.
    try:
        id_num = int(''.join(filter(str.isdigit, args.id)))
        calculated_port = 8090 + (id_num - 1)  # UAV-001 -> 8090, UAV-002 -> 8091 ...
    except:
        calculated_port = 8090

    print("🎬 KAMERA VE AI SİSTEMİ BAŞLATILIYOR...")
    print(f"🚁 KİMLİK: {args.id}")
    print(f"🔌 PORT  : {calculated_port}")
    print(f"📡 LİNK  : http://127.0.0.1:{calculated_port}/video_feed")

    t = threading.Thread(target=start_flask, args=(calculated_port,))
    t.daemon = True
    t.start()
    
    start_uav_vision(args.video, args.id, args.name)
        console.log("🔄 Dashboard başlatılıyor...");

            // =========================================================
        // 🎥 DİNAMİK CANLI VİDEO YÖNETİMİ (KOPYALA YAPIŞTIR)
        // =========================================================
        const videoImg = document.getElementById('liveVideo');
        const noSignal = document.getElementById('noSignal');
        const BASE_PORT = 8090; // Video portu (GCS veri portu 5050'den ayrı). UAV-001 -> 8090, UAV-002 -> 8091...
        let currentVideoPort = null;
        let videoRefreshInterval = null;
        let snapshotInterval = null; // Fotoğraf çekme zamanlayıcısı

        // --- PORT HESAPLAYICI ---
        function getPortFromUavId(uavId) {
            try {
                // "UAV-001" içindeki "1"i alır
                const idNumber = parseInt(uavId.replace(/\D/g, ''));
                if (isNaN(idNumber)) return BASE_PORT;
                // Mantık: BasePort + (ID - 1)
                return BASE_PORT + (idNumber - 1);
            } catch (e) {
                console.error("Port hesaplama hatası:", e);
                return BASE_PORT;
            }
        }

        // --- VİDEO YÜKLEME FONKSİYONU ---
                function loadVideo(port) {
            if (!port) return;

            // Eğer eski bir döngü varsa önce onu temizle
            if (snapshotInterval) clearInterval(snapshotInterval);

            // Sinyal yazısını gizle, resmi aç
            noSignal.style.setProperty('display', 'none', 'important');
            videoImg.style.display = 'block';

            // Saniyede 10 kere (100ms) yeni resim çekmek için döngü kuruyoruz
            snapshotInterval = setInterval(() => {
                const uniqueId = new Date().getTime(); // Tarayıcı hafızasını kandırmak için
                // DİKKAT: Artık /video_feed değil /snapshot adresine gidiyoruz
                videoImg.src = `http://127.0.0.1:${port}/snapshot?r=${uniqueId}`;
            }, 100);
        }

        // --- VİDEO EVENTLERİ ---
        videoImg.onload = function() {
            noSignal.style.setProperty('display', 'none', 'important');
            videoImg.style.display = 'block';
            if(videoRefreshInterval) clearTimeout(videoRefreshInterval);
        };

        videoImg.onerror = function() {
            videoImg.style.display = 'none';

            // Eğer bir İHA seçiliyse "Sinyal Aranıyor" göster
            if (currentVideoPort) {
                 noSignal.innerHTML = `
                    <div class="spinner-grow text-danger mb-3" role="status" style="width: 3rem; height: 3rem;"></div>
                    <h5 class="text-danger fw-bold">SİNYAL ARANIYOR...</h5>
                    <small class="text-white">Bağlantı bekleniyor (Port ${currentVideoPort})</small>
                `;
                noSignal.style.setProperty('display', 'flex', 'important');

                // 1 saniye sonra tekrar dene
                if(videoRefreshInterval) clearTimeout(videoRefreshInterval);
                videoRefreshInterval = setTimeout(() => loadVideo(currentVideoPort), 1000);
            }
        };

       

        // ===== GLOBAL DEĞİŞKENLER =====
                // Aynı plakayı 50 kere alt alta yazmamak için hafıza
        let detectedPlatesMemory = new Set();
        let totalDetections = 0;
        let threatCount = 0;
        let confidenceSum = 0;
        let confidenceCount = 0;
        let sessionStartTime = Date.now();
        let lastThreatAlertTime = 0;
        let allTargets = [];
        let currentMapFilter = 'all';
        let currentTableFilter = 'all';
        // ===== UAV BAZLI İSTATİSTİKLER =====
        let uavStats = {}; // Her UAV için ayrı istatistik tutacak

        // Hedef tipi sayaçları
        let targetTypeCounts = {
            car: 0, truck: 0, bus: 0, person: 0,
            bicycle: 0, motorcycle: 0, other: 0
        };

        // Zaman serisi
        let timeSeriesData = { labels: [], data: [] };
        let detectionsPer10Sec = 0;

                // ===== UAV YÖNETİMİ =====
        let connectedUavs = {};
        let currentUavFilter = 'all';
        let uavColors = {
            'UAV-001': '#3498db',
            'UAV-002': '#e74c3c',
            'UAV-003': '#2ecc71',
            'UAV-004': '#9b59b6',
            'UAV-005': '#f39c12',
            'default': '#95a5a6'
        };

        // ===== HARİTA BAŞLAT =====
        const map = L.map('map').setView([36.2017, 36.1611], 13);
        // Harita altlığı canlı OpenStreetMap'ten yüklenir (internet gerekir).
        // İnternetsiz ortamda altlık boş/gri kalır ama işaretçiler doğru konumda görünür.
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap',
            maxZoom: 19
        }).addTo(map);

        const markers = L.layerGroup().addTo(map);

            // ==========================================
        // 🕵️ ARANAN PLAKA YÖNETİMİ
        // ==========================================
        let wantedPlates = ["34ABC12"]; // Varsayılan liste

        function renderWantedList() {
            const container = document.getElementById('wantedListBadges');
            container.innerHTML = '';

            if (wantedPlates.length === 0) {
                container.innerHTML = '<span class="badge bg-secondary">Liste Boş</span>';
                return;
            }

            wantedPlates.forEach(plate => {
                const badge = document.createElement('span');
                badge.className = 'badge bg-danger me-1';
                badge.innerText = plate;
                container.appendChild(badge);
            });

            // Listeyi Python'a Gönder (Senkronize Et)
            syncPlatesWithPython();
        }

        function addWantedPlate() {
            const input = document.getElementById('newPlateInput');
            // Boşlukları sil, büyük harf yap
            const plate = input.value.replace(/\s/g, '').toUpperCase();

            if (plate && !wantedPlates.includes(plate)) {
                wantedPlates.push(plate);
                renderWantedList();
                input.value = ''; // Kutuyu temizle
            } else {
                alert("Geçersiz veya zaten ekli plaka!");
            }
        }

        function clearWantedList() {
            if(confirm("Tüm arananlar listesi silinsin mi?")) {
                wantedPlates = [];
                renderWantedList();
            }
        }

        // Python Modülüne Listeyi Gönderen Fonksiyon
        function syncPlatesWithPython() {
            // Listeyi BAĞLI TÜM UAV'ların Python portlarına gönder (sadece seçili olana değil).
            const ports = new Set();
            Object.keys(connectedUavs).forEach(id => ports.add(getPortFromUavId(id)));
            if (currentVideoPort) ports.add(currentVideoPort);

            if (ports.size === 0) {
                console.warn("⚠️ Henüz bağlı UAV yok; plaka listesi UAV bağlanınca gönderilecek.");
                return;
            }

            ports.forEach(port => {
                fetch(`http://127.0.0.1:${port}/update_wanted`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ plates: wantedPlates })
                })
                .then(response => response.json())
                .then(data => console.log(`✅ Plaka listesi gönderildi (port ${port}):`, data))
                .catch(error => console.warn(`⚠️ Python'a ulaşılamadı (port ${port}):`, error));
            });
        }

        // Sayfa açılınca mevcut listeyi göster
        document.addEventListener("DOMContentLoaded", function() {
            renderWantedList();
        });

        // İnternetsiz ortamda dış PNG ikon indirilemez; renkli CSS daireleri kullanıyoruz.
        const MARKER_COLORS = {
            red: '#dc3545', blue: '#3498db', green: '#28a745', grey: '#95a5a6'
        };
        function createCircleMarker(latlng, color) {
            return L.circleMarker(latlng, {
                radius: 8,
                color: '#ffffff',
                weight: 2,
                fillColor: MARKER_COLORS[color] || MARKER_COLORS.grey,
                fillOpacity: 0.9
            });
        }

        // ===== FİLTRELEME FONKSİYONLARI =====
                function shouldShowTarget(target, filter) {
            // UAV filtresi kontrol
            if (currentUavFilter !== 'all') {
                if (target.uavId && target.uavId !== currentUavFilter) {
                    return false;
                }
            }

            // Tip filtresi
            const type = (target.type || '').toLowerCase();
            switch (filter) {
                case 'all': return true;
                case 'car': return ['car', 'truck', 'bus'].includes(type);
                case 'person': return type === 'person';
                case 'threat': return target.isThreat === true;
                default: return true;
            }
        }

        function refreshMapMarkers() {
            markers.clearLayers();
            allTargets.forEach(target => {
                if (shouldShowTarget(target, currentMapFilter)) {
                    addMarkerToMap(target, false); // timeout yok
                }
            });
        }

        function addMarkerToMap(target, autoRemove = true) {
            if (!target.lat || !target.lon) return null;

            const type = (target.type || '').toLowerCase();
            let markerColor = 'grey';

            if (target.isThreat) {
                markerColor = 'red';
            } else if (['car', 'truck', 'bus'].includes(type)) {
                markerColor = 'blue';
            } else if (type === 'person') {
                markerColor = 'green';
            }

            const marker = createCircleMarker([target.lat, target.lon], markerColor)
                .bindPopup(`
                    <b>ID: ${target.trackId} - ${(target.type || '').toUpperCase()}</b><br>
                    Güven: ${(target.confidence * 100).toFixed(1)}%<br>
                    Tehdit: ${target.isThreat ? '⚠️ EVET' : '✅ HAYIR'}
                `)
                .addTo(markers);

            if (autoRemove) {
                setTimeout(() => marker.remove(), 15000);
            }

            return marker;
        }

        function applyTableFilter() {
            const filter = currentTableFilter;
            document.querySelectorAll('#targetTable tbody tr').forEach(row => {
                const type = row.dataset.type || '';
                const isThreat = row.dataset.threat === 'true';
                const target = { type: type, isThreat: isThreat };
                row.style.display = shouldShowTarget(target, filter) ? '' : 'none';
            });
        }

        // ===== HARİTA FİLTRE BUTONLARI =====
        document.querySelectorAll('#mapFilterGroup button').forEach(btn => {
            btn.addEventListener('click', function() {
                // Aktif butonu güncelle
                document.querySelectorAll('#mapFilterGroup button').forEach(b => {
                    b.classList.remove('active', 'btn-light');
                    b.classList.add('btn-outline-light');
                });
                this.classList.remove('btn-outline-light');
                this.classList.add('active', 'btn-light');

                // Filtreyi uygula
                currentMapFilter = this.dataset.filter;
                refreshMapMarkers();
            });
        });

        // ===== TABLO FİLTRESİ =====
        document.getElementById('tableFilter').addEventListener('change', function() {
            currentTableFilter = this.value;
            applyTableFilter();
        });

        // ===== CHART.JS =====
        const targetTypeCtx = document.getElementById('targetTypeChart').getContext('2d');
        const targetTypeChart = new Chart(targetTypeCtx, {
            type: 'doughnut',
            data: {
                labels: ['Araba', 'Kamyon', 'Otobüs', 'İnsan', 'Bisiklet', 'Motosiklet', 'Diğer'],
                datasets: [{
                    data: [0, 0, 0, 0, 0, 0, 0],
                    backgroundColor: ['#3498db', '#e74c3c', '#f39c12', '#2ecc71', '#9b59b6', '#1abc9c', '#95a5a6'],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12, padding: 8, font: { size: 10 } } }
                }
            }
        });

        const timeSeriesCtx = document.getElementById('timeSeriesChart').getContext('2d');
        const timeSeriesChart = new Chart(timeSeriesCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Tespit/10sn',
                    data: [],
                    borderColor: '#3498db',
                    backgroundColor: 'rgba(52, 152, 219, 0.1)',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true } },
                plugins: { legend: { display: false } }
            }
        });

        // Zaman serisi güncelle
        setInterval(() => {
            const now = new Date();
            const timeLabel = now.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

            timeSeriesData.labels.push(timeLabel);
            timeSeriesData.data.push(detectionsPer10Sec);

            if (timeSeriesData.labels.length > 30) {
                timeSeriesData.labels.shift();
                timeSeriesData.data.shift();
            }

            timeSeriesChart.data.labels = timeSeriesData.labels;
            timeSeriesChart.data.datasets[0].data = timeSeriesData.data;
            timeSeriesChart.update('none');

            detectionsPer10Sec = 0;
        }, 10000);

        // ===== TELEMETRY GÜNCELLEME =====
        function updateTelemetry(data) {
            // Batarya
            const battery = data.battery || 0;
            const batteryArc = document.getElementById('batteryArc');
            const batteryValue = document.getElementById('batteryValue');
            const arcLength = (battery / 100) * 126;
            batteryArc.style.strokeDasharray = `${arcLength}, 126`;
            batteryArc.style.stroke = battery > 50 ? '#28a745' : battery > 20 ? '#ffc107' : '#dc3545';
            batteryValue.textContent = `${battery.toFixed(0)}%`;

            // İrtifa
            const altitude = data.altitude || 0;
            document.getElementById('altitudeBar').style.height = `${Math.min((altitude / 200) * 100, 100)}%`;
            document.getElementById('altitudeValue').textContent = `${altitude.toFixed(1)}m`;

            // Hız
            const speed = data.speed || 0;
            const speedArc = document.getElementById('speedArc');
            speedArc.style.strokeDasharray = `${(speed / 80) * 126}, 126`;
            document.getElementById('speedValue').textContent = speed.toFixed(1);

            // Pusula
            const heading = data.heading || 0;
            document.getElementById('compassArrow').style.transform = `translate(-50%, -100%) rotate(${heading}deg)`;
            document.getElementById('headingValue').textContent = `${heading.toFixed(0)}°`;

            // Sinyal
            const signalStrength = data.signalStrength || 0;
            document.getElementById('signalValue').textContent = data.signal || '--';

            const signalBars = ['sig1', 'sig2', 'sig3', 'sig4', 'sig5'].map(id => document.getElementById(id));
            const activeBars = Math.ceil((signalStrength / 100) * 5);
            signalBars.forEach((bar, i) => {
                bar.className = 'signal-bar';
                if (i < activeBars) {
                    bar.classList.add(signalStrength > 60 ? 'active' : signalStrength > 30 ? 'warning' : 'danger');
                }
            });

            // Uçuş Süresi
            const flightTime = data.flightTime || 0;
            const minutes = Math.floor(flightTime / 60);
            const seconds = Math.floor(flightTime % 60);
            document.getElementById('flightTimeValue').textContent =
                `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        }

        // ===== İSTATİSTİK GÜNCELLEME =====
        function updateStats() {
            document.getElementById('totalCount').textContent = totalDetections;
            document.getElementById('threatCount').textContent = threatCount;

            const avgConf = confidenceCount > 0 ? (confidenceSum / confidenceCount * 100).toFixed(1) : 0;
            document.getElementById('avgConfidence').textContent = avgConf;

            const elapsedMinutes = (Date.now() - sessionStartTime) / 60000;
            document.getElementById('ratePerMin').textContent = elapsedMinutes > 0 ? (totalDetections / elapsedMinutes).toFixed(1) : 0;

            targetTypeChart.data.datasets[0].data = [
                targetTypeCounts.car, targetTypeCounts.truck, targetTypeCounts.bus,
                targetTypeCounts.person, targetTypeCounts.bicycle, targetTypeCounts.motorcycle,
                targetTypeCounts.other
            ];
            targetTypeChart.update('none');
        }

                // ===== FİLTRELENMİŞ İSTATİSTİK GÜNCELLEME =====
        function refreshStats() {
            let stats;

            if (currentUavFilter === 'all') {
                // Tümü seçiliyse global istatistikleri hesapla
                stats = {
                    totalDetections: 0,
                    threatCount: 0,
                    confidenceSum: 0,
                    confidenceCount: 0,
                    targetTypeCounts: { car: 0, truck: 0, bus: 0, person: 0, bicycle: 0, motorcycle: 0, other: 0 }
                };

                // Tüm UAV istatistiklerini topla
                Object.values(uavStats).forEach(uav => {
                    stats.totalDetections += uav.totalDetections;
                    stats.threatCount += uav.threatCount;
                    stats.confidenceSum += uav.confidenceSum;
                    stats.confidenceCount += uav.confidenceCount;

                    Object.keys(uav.targetTypeCounts).forEach(type => {
                        stats.targetTypeCounts[type] += uav.targetTypeCounts[type];
                    });
                });
            } else {
                // Spesifik UAV seçiliyse sadece o UAV'ın istatistikleri
                stats = uavStats[currentUavFilter] || {
                    totalDetections: 0,
                    threatCount: 0,
                    confidenceSum: 0,
                    confidenceCount: 0,
                    targetTypeCounts: { car: 0, truck: 0, bus: 0, person: 0, bicycle: 0, motorcycle: 0, other: 0 }
                };
            }

            // UI Güncelle
            document.getElementById('totalCount').textContent = stats.totalDetections;
            document.getElementById('threatCount').textContent = stats.threatCount;

            const avgConf = stats.confidenceCount > 0
                ? (stats.confidenceSum / stats.confidenceCount * 100).toFixed(1)
                : 0;
            document.getElementById('avgConfidence').textContent = avgConf;

            const elapsedMinutes = (Date.now() - sessionStartTime) / 60000;
            document.getElementById('ratePerMin').textContent = elapsedMinutes > 0
                ? (stats.totalDetections / elapsedMinutes).toFixed(1)
                : 0;

            // Grafik güncelle
            targetTypeChart.data.datasets[0].data = [
                stats.targetTypeCounts.car,
                stats.targetTypeCounts.truck,
                stats.targetTypeCounts.bus,
                stats.targetTypeCounts.person,
                stats.targetTypeCounts.bicycle,
                stats.targetTypeCounts.motorcycle,
                stats.targetTypeCounts.other
            ];
            targetTypeChart.update('none');
        }

        // ===== TEHDİT ALARMI =====
        function showThreatAlert(message, details) {
            const now = Date.now();
            if (now - lastThreatAlertTime > 10000) {
                document.getElementById('threatMessage').textContent = message;
                document.getElementById('threatDetails').textContent = details;
                new bootstrap.Modal(document.getElementById('threatModal')).show();
                lastThreatAlertTime = now;
            }
        }

        // ==========================================
        // ⚠️ YOĞUNLUK TEHDİDİ (Tehdit Analiz Eşiği)
        // ==========================================
        // Aynı anda sahnede AKTİF araç sayısı, slider'daki eşiği aşarsa sarı alarm verir.
        // (Aranan plaka = kırmızı tehdit; yoğunluk = sarı tehdit — ikisi ayrıdır.)
        let recentVehicles = {};        // "uavId:trackId" -> son görülme zamanı (ms)
        let lastDensityAlertTime = 0;
        const DENSITY_WINDOW_MS = 4000; // son 4 sn içinde görülen araç "aktif" sayılır

        function updateDensity(data) {
            const type = (data.type || '').toLowerCase();
            if (!['car', 'truck', 'bus'].includes(type)) return;
            const key = (data.uavId || 'UAV-001') + ':' + data.trackId;
            recentVehicles[key] = Date.now();
        }

        function getActiveVehicleCount() {
            const now = Date.now();
            let count = 0;
            for (const k in recentVehicles) {
                if (now - recentVehicles[k] <= DENSITY_WINDOW_MS) count++;
                else delete recentVehicles[k];  // eski kayıtları temizle
            }
            return count;
        }

        function addDensityToThreatLog(active, threshold) {
            const tbody = document.getElementById('threatLogBody');
            const row = document.createElement('tr');
            const now = new Date().toLocaleTimeString();
            row.innerHTML = `
                <td><small class="text-muted fw-bold">${now}</small></td>
                <td><span class="badge bg-secondary">FİLO</span></td>
                <td><span class="badge bg-warning text-dark">⚠️ YOĞUNLUK</span></td>
                <td><i class="bi bi-car-front-fill"></i> ${active} araç (eşik: ${threshold})</td>
            `;
            tbody.prepend(row);
            row.style.animation = "highlightRow 1s";
        }

        // Her 1.5 sn'de bir yoğunluğu kontrol et + eşik rozetinde canlı göster
        setInterval(() => {
            const active = getActiveVehicleCount();
            const threshold = parseInt(document.getElementById('threatThresholdRange').value);

            const badge = document.getElementById('thresholdDisplay');
            badge.innerText = `Eşik ${threshold} | Aktif ${active}`;
            // Eşik aşıldıysa rozeti kırmızı yak, değilse normal
            badge.className = active >= threshold ? 'badge bg-danger fs-6' : 'badge bg-secondary fs-6';

            if (active >= threshold) {
                const now = Date.now();
                if (now - lastDensityAlertTime > 8000) {  // 8 sn'de bir en fazla bir alarm
                    lastDensityAlertTime = now;
                    showThreatAlert(`⚠️ YOĞUN ARAÇ HAREKETİ! (${active} araç)`, `Belirlenen eşik (${threshold} araç) aşıldı.`);
                    addDensityToThreatLog(active, threshold);
                }
            }
        }, 1500);

        // ===== SIGNALR =====
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/targetHub")
            .configureLogging(signalR.LogLevel.Information)
            .build();

            connection.on("ReceiveTarget", function(json) {
                try {
                    const data = JSON.parse(json);

                    // 1. ADIM: GPS koordinatlarını çöz ve veriyi hafızaya kaydet.
                    //          (Filtreden bağımsız — "Tümü" seçilince geçmiş de görünür.)
                    let gpsLat = null, gpsLon = null, gpsText = "N/A";
                    if (data.gpsCoords && data.gpsCoords !== "null") {
                        try {
                            const gpsArray = typeof data.gpsCoords === 'string' ? JSON.parse(data.gpsCoords) : data.gpsCoords;
                            gpsLat = parseFloat(gpsArray[0]);
                            gpsLon = parseFloat(gpsArray[1]);
                            gpsText = `${gpsLat.toFixed(6)}, ${gpsLon.toFixed(6)}`;
                        } catch (e) {
                            console.error("GPS parse hatası:", e);
                        }
                    }
                    // İşaretçiler ve geçmiş oynatma için lat/lon'u objeye göm.
                    data.lat = gpsLat;
                    data.lon = gpsLon;
                    data.plate = data.plate || "BELIRSIZ";

                    allTargets.push(data);
                    if (allTargets.length > 500) allTargets.shift();

                    // 2. ADIM: UAV LİSTESİNİ GÜNCELLE (Her zaman çalışmalı)
                    updateUavList(data.uavId || 'UAV-001', data.uavName || 'Alpha-1');
                    const uavId = data.uavId || 'UAV-001';
                            if (uavStats[uavId]) {
                                uavStats[uavId].totalDetections++;
                                if (data.isThreat) uavStats[uavId].threatCount++;
                                uavStats[uavId].confidenceSum += data.confidence;
                                uavStats[uavId].confidenceCount++;

                                const type = (data.type || '').toLowerCase();
                                if (uavStats[uavId].targetTypeCounts.hasOwnProperty(type)) {
                                    uavStats[uavId].targetTypeCounts[type]++;
                                } else {
                                    uavStats[uavId].targetTypeCounts.other++;
                                }
                            }

                    // Yoğunluk sayacını güncelle (filtreden bağımsız - tüm araçları say)
                    updateDensity(data);

                    // 3. ADIM: KRİTİK FİLTRE KONTROLÜ
                    // Eğer bir İHA seçiliyse ve gelen veri o İHA'ya ait değilse BURADA DUR.
                    if (currentUavFilter !== 'all' && data.uavId !== currentUavFilter) {
                        return; // Fonksiyonu bitir, aşağıya (UI güncellemeye) geçme.
                    }

                    // --- BURADAN AŞAĞISI SADECE SEÇİLİ İHA İÇİN ÇALIŞACAK ---

                    // Toplam veri sayısını güncelle
                    document.getElementById('totalDataCount').innerHTML =
                        `<i class="bi bi-database"></i> ${totalDetections + 1} Veri`;

                    console.log("✅ Ekrana Basılan Veri:", data);

                    document.getElementById('status').className = 'badge bg-success me-2';
                    document.getElementById('status').textContent = 'AKTİF AKIŞ';

                    // İstatistikler (Seçili İHA'ya göre güncellenir)
                    totalDetections++;
                    detectionsPer10Sec++;
                    if (data.isThreat) threatCount++;
                    confidenceSum += data.confidence;
                    confidenceCount++;

                    // Hedef tipi sayacı
                    const type = (data.type || '').toLowerCase();
                    if (targetTypeCounts.hasOwnProperty(type)) {
                        targetTypeCounts[type]++;
                    } else {
                        targetTypeCounts.other++;
                    }

                    refreshStats();
                    updateTelemetry(data);
                    updateTelemetryStats(data);

                // Tabloya ekleme kodun buraya devam etsin...

                // GPS zaten yukarıda çözüldü (gpsLat / gpsLon / gpsText).

                // Tehdit olayını kaydet
                if (data.isThreat) {
                    recordThreatEvent(gpsText);
                }

                // Haritaya ekle (data artık lat/lon içeriyor)
                if (gpsLat && gpsLon && shouldShowTarget(data, currentMapFilter)) {
                    addMarkerToMap(data, true);
                }

                // İlk tespit - haritayı ortala
                if (totalDetections === 1 && gpsLat && gpsLon) {
                    map.setView([gpsLat, gpsLon], 14);
                }

                // Tablo satırı
                const timestamp = new Date(data.timestamp);
                const timeStr = timestamp.toLocaleTimeString('tr-TR');
                const rowClass = data.isThreat ? "table-danger" : "table-success";

                const row = document.createElement('tr');
                row.className = `${rowClass} animate-row`;
                row.dataset.type = type;
                row.dataset.threat = String(data.isThreat);
                row.dataset.uav = data.uavId || 'UAV-001';
                    // Plaka varsa rozet hazırla
            const plateBadge = (data.plate && data.plate !== "BELIRSIZ")
                ? `<br><span class="badge bg-warning text-dark mt-1" style="font-size: 0.75em;">🚗 ${data.plate}</span>`
                : '';

            row.innerHTML = `
            <td><span class="badge" style="background:${getUavColor(data.uavId || 'UAV-001')}">${data.uavName || 'UAV'}</span></td>
            <td><strong>#${data.trackId}</strong></td>
            <td>${timeStr}</td>
            <td>
                <span class="badge bg-dark">${(data.type || '').toUpperCase()}</span>
                ${plateBadge} </td>
            <td><span class="badge bg-info text-dark">${(data.confidence * 100).toFixed(1)}%</span></td>
            <td>${data.isThreat ? '<span class="badge bg-danger">⚠️ EVET</span>' : '<span class="badge bg-success">✅ HAYIR</span>'}</td>
        `;

                // Filtre kontrolü
                if (!shouldShowTarget({ type: type, isThreat: data.isThreat }, currentTableFilter)) {
                    row.style.display = 'none';
                }

                const tbody = document.querySelector('#targetTable tbody');
                tbody.insertBefore(row, tbody.firstChild);

                setTimeout(() => row.classList.remove(rowClass), 2000);

                while (tbody.children.length > 50) {
                    tbody.removeChild(tbody.lastChild);
                }

                // Tehdit alarmı
                // 🚨 Tehdit ve Plaka Alarmı
                if (data.isThreat) {

                    // Eğer plaka varsa, özel bir mesaj göster
                    if (data.plate && data.plate !== "BELIRSIZ") {
                        showThreatAlert("🚨 ARANAN PLAKA TESPİT EDİLDİ!", `Plaka: ${data.plate} | İHA: ${data.uavName}`);

                        // Aşağıdaki listeye ekle (Daha önce verdiğim fonksiyon)
                        addToThreatLog(data);
                    }
                    else {
                        // Standart kalabalık/tehdit uyarısı
                        showThreatAlert("Bölgede yoğun araç hareketi!", `${data.type} - Konum: ${gpsText}`);
                    }
                }

            } catch (error) {
                console.error("❌ Veri işleme hatası:", error);
            }
        });


                // Slider hareket ettikçe değeri ekranda göster
        document.getElementById('threatThresholdRange').addEventListener('input', function() {
            document.getElementById('thresholdDisplay').innerText = this.value + " Araç";
        });

        // Slider bırakıldığında Python'a (Backend üzerinden) komut gönder
        document.getElementById('threatThresholdRange').addEventListener('change', function() {
            fetch(`/Report/UpdateThreshold?value=${this.value}`, { method: 'POST' });
        });

        connection.start()
            .then(() => console.log("✅ SignalR bağlantısı başarılı!"))
            .catch(err => {
                console.error("❌ SignalR hatası:", err);
                document.getElementById('status').className = 'badge bg-danger me-2';
                document.getElementById('status').textContent = 'BAĞLANTI HATASI';
            });


                // ===== UAV FONKSİYONLARI =====
        function getUavColor(uavId) {
            return uavColors[uavId] || uavColors['default'];
        }

                function updateUavList(uavId, uavName) {
            if (!connectedUavs[uavId]) {
                connectedUavs[uavId] = { name: uavName, lastSeen: Date.now(), dataCount: 0 };

                // UAV için istatistik objesi oluştur
                uavStats[uavId] = {
                    totalDetections: 0,
                    threatCount: 0,
                    confidenceSum: 0,
                    confidenceCount: 0,
                    targetTypeCounts: { car: 0, truck: 0, bus: 0, person: 0, bicycle: 0, motorcycle: 0, other: 0 }
                };

                const color = getUavColor(uavId);
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-outline-primary btn-sm';
                btn.dataset.uav = uavId;
                btn.innerHTML = `<i class="bi bi-airplane" style="color:${color}"></i> ${uavName}`;
                btn.addEventListener('click', function() { filterByUav(uavId, this); });
                document.getElementById('uavSelector').appendChild(btn);

                console.log(`🚁 Yeni UAV bağlandı: ${uavName} (${uavId})`);

                // İlk bağlanan UAV'ın canlı videosunu panelde OTOMATİK göster
                // (kullanıcı üstten manuel seçmese bile video akmaya başlasın).
                if (!currentVideoPort) {
                    currentVideoPort = getPortFromUavId(uavId);
                    console.log(`🎥 Video otomatik açılıyor: ${uavId} -> Port ${currentVideoPort}`);
                    loadVideo(currentVideoPort);
                }

                // Yeni bağlanan UAV'a güncel aranan-plaka listesini gönder
                syncPlatesWithPython();
            }

            connectedUavs[uavId].lastSeen = Date.now();
            connectedUavs[uavId].dataCount++;

            document.getElementById('activeUavCount').innerHTML =
                `<i class="bi bi-broadcast"></i> ${Object.keys(connectedUavs).length} UAV Bağlı`;
        }

        function filterByUav(uavId, clickedBtn) {
                currentUavFilter = uavId;

                // 1. Tablo ve Harita Temizliği (Eski Kodun Aynısı)
                document.querySelector('#targetTable tbody').innerHTML = "";
                markers.clearLayers();

                // Buton Görselliği (Eski Kodun Aynısı)
                document.querySelectorAll('#uavSelector button').forEach(btn => {
                    btn.classList.remove('active', 'btn-primary');
                    btn.classList.add('btn-outline-primary');
                });
                if (clickedBtn) {
                    clickedBtn.classList.remove('btn-outline-primary');
                    clickedBtn.classList.add('active', 'btn-primary');
                }

                // >>>>>> YENİ EKLENEN KISIM: VİDEO DEĞİŞTİRME <<<<<<
                if (uavId === 'all') {
                    // Tümü seçiliyse videoyu kapat
                    currentVideoPort = null;
                    videoImg.style.display = 'none';
                    videoImg.src = ""; // Bağlantıyı kopar
                    noSignal.style.setProperty('display', 'flex', 'important');
                    noSignal.innerHTML = `
                        <i class="bi bi-camera-reels text-secondary mb-3" style="font-size: 3rem;"></i>
                        <h5 class="text-white fw-bold">KAMERA SEÇİN</h5>
                        <small class="text-white">Görüntülemek için yukarıdan bir UAV seçin</small>
                    `;
                } else {
                    // Spesifik UAV seçildiyse portu hesapla ve aç
                    const newPort = getPortFromUavId(uavId);
                    console.log(`🎥 Video Kanalı Değişiyor: ${uavId} -> Port ${newPort}`);

                    if (currentVideoPort !== newPort) {
                        currentVideoPort = newPort;
                        videoImg.style.display = 'none';
                        noSignal.innerHTML = `
                            <div class="spinner-grow text-danger mb-3" role="status"></div>
                            <h5 class="text-danger">KANAL DEĞİŞTİRİLİYOR...</h5>
                        `;
                        noSignal.style.setProperty('display', 'flex', 'important');

                        loadVideo(newPort);
                    }
                }
                // >>>>>> VİDEO KISMI BİTTİ <<<<<<

                // 2. Geçmiş verileri filtreleme (Eski Kodun Aynısı)
                allTargets.forEach(target => {
                    if (currentUavFilter === 'all' || target.uavId === currentUavFilter) {
                        if (target.lat && target.lon) {
                            addMarkerToMap(target, false);
                        }
                        // ... Tablo ekleme kodları aynen devam ediyor ...
                         const type = (target.type || '').toLowerCase();
                        const row = document.createElement('tr');
                        row.className = target.isThreat ? "table-danger" : "table-success";
                        // ... (Satır oluşturma kodların buraya) ...
                        // HTML bloğunu şişirmemek için burayı kısalttım, senin kodun aynısı kalacak.
                         row.innerHTML = `
                                <td><span class="badge" style="background:${getUavColor(target.uavId || 'UAV-001')}">${target.uavName || 'UAV'}</span></td>
                                <td><strong>#${target.trackId}</strong></td>
                                <td>--:--</td>
                                <td><span class="badge bg-dark">${(target.type || '').toUpperCase()}</span></td>
                                <td><span class="badge bg-info text-dark">${((target.confidence || 0) * 100).toFixed(1)}%</span></td>
                                <td>${target.isThreat ? '<span class="badge bg-danger">⚠️ EVET</span>' : '<span class="badge bg-success">✅ HAYIR</span>'}</td>
                            `;
                        document.querySelector('#targetTable tbody').appendChild(row);
                    }
                });

                refreshStats();

                console.log(`🔍 UAV filtresi: ${uavId}`);
            }

            // SAYFA İLK AÇILDIĞINDA
            // "Tümü" seçili olduğu için video kapalı başlar.
            // Eğer ilk UAV otomatik açılsın istersen burayı değiştirebiliriz.
        // Tümü butonuna event listener
        document.querySelector('#uavSelector button[data-uav="all"]').addEventListener('click', function() {
            filterByUav('all', this);
        });


        connection.onclose(() => {
            document.getElementById('status').className = 'badge bg-warning text-dark me-2';
            document.getElementById('status').textContent = 'BAĞLANTI KESİLDİ';
            setTimeout(() => connection.start(), 5000);
        });

                // ===== RAPOR SİSTEMİ =====
        let missionStartTime = new Date();
        let maxAltitude = 0;
        let maxSpeed = 0;
        let minBattery = 100;
        let threatEvents = [];

        // Görev başlangıç zamanını göster
        document.getElementById('missionStartTime').textContent = missionStartTime.toLocaleTimeString('tr-TR');

        // Telemetri max/min takibi
        function updateTelemetryStats(data) {
            if ((data.altitude || 0) > maxAltitude) maxAltitude = data.altitude;
            if ((data.speed || 0) > maxSpeed) maxSpeed = data.speed;
            if ((data.battery || 100) < minBattery) minBattery = data.battery;
        }

        // Tehdit olayı kaydet
        function recordThreatEvent(gpsText) {
            // Slider'daki güncel değeri oku
            const currentThreshold = parseInt(document.getElementById('threatThresholdRange').value);

            threatEvents.push({
                time: new Date().toISOString(),
                location: gpsText,
                vehicleCount: currentThreshold // ARTIK STATİK 3 DEĞİL, SEÇTİĞİN DEĞER!
            });
        }

        // Rapor verisi oluştur
                function buildReportData() {
            // 1. İHA Bazlı Detayları Hazırla (Multi-UAV Dictionary)
            let uavReports = {};
            Object.keys(connectedUavs).forEach(id => {
                const uavData = allTargets.filter(t => t.uavId === id);
                uavReports[id] = {
                    uavName: connectedUavs[id].name,
                    detections: uavData.length,
                    threats: uavData.filter(t => t.isThreat).length,
                    avgConf: uavData.length > 0
                        ? (uavData.reduce((s, t) => s + (t.confidence || 0), 0) / uavData.length) * 100
                        : 0
                };
            });

            // 2. En Detaylı Rapor Objesini Döndür
            return {
                // Zaman Bilgisi
                missionStart: missionStartTime.toISOString(),

                // Genel İstatistikler (Eski Detaylar Geri Geldi ✅)
                totalDetections: totalDetections,
                threatCount: threatCount,
                averageConfidence: confidenceCount > 0 ? (confidenceSum / confidenceCount * 100) : 0,
                targetTypeCounts: targetTypeCounts, // car, person, truck sayıları

                // Telemetri Limitleri
                maxAltitude: maxAltitude,
                maxSpeed: maxSpeed,
                minBattery: minBattery,

                // Lokasyon Bilgisi
                lastLatitude: allTargets.length > 0 ? (allTargets[allTargets.length - 1].lat || 0) : 0,
                lastLongitude: allTargets.length > 0 ? (allTargets[allTargets.length - 1].lon || 0) : 0,

                // Tehdit Olay Kayıtları (Son 10 olay)
                threatEvents: threatEvents.slice(-10),

                // Çoklu İHA Detayları (Dictionary Yapısı ✅)
                uavSpecificReports: uavReports
            };
        }

            // PDF İndir
            document.getElementById('btnDownloadPdf').addEventListener('click', async () => {
                try {
                    const response = await fetch('/Report/GeneratePdf', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(buildReportData())
                    });

                    if (response.ok) {
                        const blob = await response.blob();
                        const url = window.URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = `GorevRaporu_${new Date().toISOString().slice(0,10)}.pdf`;
                        a.click();
                        window.URL.revokeObjectURL(url);
                    } else {
                        alert('PDF oluşturma hatası!');
                    }
                } catch (error) {
                    console.error('PDF hatası:', error);
                    alert('PDF indirilemedi!');
                }
            });

            // Excel İndir
            document.getElementById('btnDownloadExcel').addEventListener('click', async () => {
                try {
                    const response = await fetch('/Report/GenerateExcel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(buildReportData())
                    });

                    if (response.ok) {
                        const blob = await response.blob();
                        const url = window.URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = `GorevRaporu_${new Date().toISOString().slice(0,10)}.csv`;
                        a.click();
                        window.URL.revokeObjectURL(url);
                    } else {
                        alert('Excel oluşturma hatası!');
                    }
                } catch (error) {
                    console.error('Excel hatası:', error);
                    alert('Excel indirilemedi!');
                }
            });

            // Görevi Bitir
            document.getElementById('btnEndMission').addEventListener('click', () => {
                if (confirm('Görevi bitirmek istediğinize emin misiniz?\nPDF raporu otomatik indirilecek.')) {
                    document.getElementById('missionStatus').className = 'badge bg-secondary ms-2';
                    document.getElementById('missionStatus').textContent = 'BİTTİ';
                    document.getElementById('btnDownloadPdf').click();
                }
            });

                // ==========================================
        // 📝 EKSİK OLAN TEHDİT GÜNLÜĞÜ FONKSİYONLARI
        // ==========================================

        function addToThreatLog(target) {
            // Eğer bu plaka daha önce listeye eklendiyse tekrar ekleme (Spam önleme)
            // Not: detectedPlatesMemory Set'i global değişkenlerde tanımlı olmalı
            if (detectedPlatesMemory.has(target.plate)) return;

            // Plakayı hafızaya al
            detectedPlatesMemory.add(target.plate);

            const tbody = document.getElementById('threatLogBody');
            const row = document.createElement('tr');

            // Şu anki saati al
            const now = new Date().toLocaleTimeString();

            row.innerHTML = `
                <td><small class="text-muted fw-bold">${now}</small></td>
                <td><span class="badge" style="background:${getUavColor(target.uavId)}">${target.uavName}</span></td>
                <td><span class="badge bg-danger fs-6">${target.plate}</span></td>
                <td><i class="bi bi-camera-video-fill text-dark"></i> ${target.type}</td>
            `;

            // En üste ekle (Prepend)
            tbody.prepend(row);

            // Yanıp sönme efekti
            row.style.animation = "highlightRow 1s";
        }

        function clearThreatLog() {
            if(confirm("Tehdit günlüğü temizlensin mi?")) {
                document.getElementById('threatLogBody').innerHTML = '';
                detectedPlatesMemory.clear(); // Hafızayı sıfırla
            }
        }

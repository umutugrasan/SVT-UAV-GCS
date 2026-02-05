using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GCS_Module.Data;
using GCS_Module.Helpers;
using GCS_Module.Hubs;
using GCS_Module.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GCS_Module.Services
{
    public class TargetListenerService : BackgroundService
    {
        private static readonly List<TcpClient> _clients = new List<TcpClient>();

        // HandleClientAsync içinde her yeni bağlantıyı listeye ekle:
        // _clients.Add(client);

        public static async Task BroadcastThreshold(int newValue)
        {
            var command = JsonSerializer.Serialize(new { type = "SET_THRESHOLD", value = newValue });
            var buffer = Encoding.UTF8.GetBytes(command + "\n");

            List<TcpClient> activeClients;

            // Listeyi kilitli şekilde kopyala
            lock (_clients)
            {
                activeClients = _clients.Where(c => c.Connected).ToList();
            }

            foreach (var client in activeClients)
            {
                try
                {
                    await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    // Yazarken hata alırsak (o an kopmuşsa) yoksay
                }
            }
        }

        private readonly IHubContext<TargetHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TargetListenerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _port;
        private TcpListener? _server;

        // Çoklu İHA takibi için thread-safe sözlük
        private readonly ConcurrentDictionary<string, DateTime> _connectedUavs = new();

        public TargetListenerService(
            IHubContext<TargetHub> hubContext,
            IConfiguration configuration,
            ILogger<TargetListenerService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _port = _configuration.GetValue<int>("Network:Port", 5050);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 SVT Çoklu İHA Dinleme Servisi Başlatılıyor...");

            try
            {
                _server = new TcpListener(IPAddress.Any, _port);
                _server.Start();
                _logger.LogInformation($"📡 TCP Server Dinlemede: Port {_port}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Bağlantıyı kabul et (using kullanmıyoruz, HandleClientAsync içinde temizlenecek)
                    TcpClient client = await _server.AcceptTcpClientAsync(stoppingToken);

                    var clientEndpoint = client.Client.RemoteEndPoint;
                    _logger.LogInformation($"🔗 Yeni İHA Bağlantısı: {clientEndpoint}");

                    // Her İHA için ayrı bir asenkron görev başlat
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogCritical(ex, "❌ TCP Server Başlatma Hatası!");
            }
            finally
            {
                _server?.Stop();
                _logger.LogInformation("🔌 TCP Server Kapatıldı.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
        {
            lock (_clients)
            {
                _clients.Add(client);
            }

            using (client)
            {
                string uavId = "Unknown";
                string uavName = "UAV-Alpha";

                try
                {
                    using NetworkStream stream = client.GetStream();
                    using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        string? encryptedMsg = await reader.ReadLineAsync();

                        if (encryptedMsg == null) break;
                        if (string.IsNullOrWhiteSpace(encryptedMsg)) continue;

                        try
                        {
                            // 1. Şifre Çözme
                            var securityHelper = new SecurityHelper(_configuration);
                            string decryptedJson = securityHelper.Decrypt(encryptedMsg);

                            // 2. JSON Çözümleme
                            var batchData = JsonSerializer.Deserialize<TargetBatch>(decryptedJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (batchData?.Targets == null || batchData.Targets.Count == 0) continue;

                            // İHA Kimlik Bilgilerini Al (İlk hedeften)
                            var first = batchData.Targets.First();
                            uavId = first.UavId ?? uavId;
                            uavName = first.UavName ?? uavName;
                            _connectedUavs[uavId] = DateTime.Now;

                            // 3. Veritabanı ve SignalR İşlemleri
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            var targets = batchData.ToTargetModels();

                            foreach (var target in targets)
                            {
                                await dbContext.Targets.AddAsync(target, stoppingToken);

                                // SIGNALR PAKETİ - TÜM TELEMETRİ VERİLERİ DAHİL EDİLDİ ✅
                                var signalRPacket = new
                                {
                                    uavId = target.UavId,
                                    uavName = target.UavName,
                                    trackId = target.TrackId,
                                    type = target.Type,
                                    plate = target.Plate,
                                    pixelCoords = target.PixelCoords,
                                    gpsCoords = target.GpsCoords,
                                    confidence = target.Confidence,
                                    isThreat = target.IsThreat,
                                    timestamp = target.Timestamp.ToString("o"),
                                    // Sol Panel Verileri
                                    battery = target.Battery,
                                    altitude = target.Altitude,
                                    speed = target.Speed,

                                    // Sağ Panel Verileri (Eksik olanlar buraya eklendi)
                                    signal = target.Signal,
                                    heading = target.Heading,
                                    signalStrength = target.SignalStrength,
                                    flightTime = target.FlightTime
                                };

                                // Web Arayüzüne Gönder
                                await _hubContext.Clients.All.SendAsync("ReceiveTarget",
                                    JsonSerializer.Serialize(signalRPacket), stoppingToken);
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"📦 [{uavName}] {targets.Count} Hedef İşlendi ve Telemetri Gönderildi.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"⚠️ [{uavName}] Paket İşleme Hatası: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ [{uavName}] Bağlantı Hatası: {ex.Message}");
                }
                finally
                {
                    // YENİ EKLENECEK KISIM (Thread-Safe Silme)
                    lock (_clients)
                    {
                        if (_clients.Contains(client))
                        {
                            _clients.Remove(client);
                        }
                    }

                    _connectedUavs.TryRemove(uavId, out _);
                    _logger.LogInformation($"🔌 [{uavName}] Bağlantısı Kesildi.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _server?.Stop();
            await base.StopAsync(cancellationToken);
        }
    }
}
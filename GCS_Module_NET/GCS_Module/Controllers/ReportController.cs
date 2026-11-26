using System;
using System.Collections.Generic;
using System.Linq;
using GCS_Module.Data;
using GCS_Module.Models;
using GCS_Module.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCS_Module.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ReportService _reportService;

        public ReportController(AppDbContext context)
        {
            _context = context;
            _reportService = new ReportService();
        }

        [HttpPost]
        public IActionResult GeneratePdf([FromBody] MissionReportRequest request)
        {
            try
            {
                // BURADA ARTIK UavReports DOLU GİDİYOR
                var report = BuildReport(request);
                var pdfBytes = _reportService.GeneratePdfReport(report);
                return File(pdfBytes, "application/pdf", $"GorevRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GenerateExcel([FromBody] MissionReportRequest request)
        {
            try
            {
                var report = BuildReport(request);
                var csvContent = BuildCsvContent(report, request.UavSpecificReports);

                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                var result = bom.Concat(csvBytes).ToArray();

                return File(result, "text/csv", $"GorevRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private MissionReport BuildReport(MissionReportRequest request)
        {
            return new MissionReport
            {
                MissionStart = request.MissionStart,
                MissionEnd = DateTime.Now,
                TotalDetections = request.TotalDetections,
                ThreatCount = request.ThreatCount,
                AverageConfidence = request.AverageConfidence,
                TargetTypeCounts = request.TargetTypeCounts ?? new Dictionary<string, int>(),
                MostDetectedType = request.TargetTypeCounts?.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "N/A",
                MaxAltitude = request.MaxAltitude,
                MaxSpeed = request.MaxSpeed,
                MinBattery = request.MinBattery,
                LastLatitude = request.LastLatitude,
                LastLongitude = request.LastLongitude,
                ThreatEvents = request.ThreatEvents ?? new List<ThreatEvent>(),

                // --- KRİTİK DÜZELTME BURASI ---
                // Request'ten gelen sözlüğü Model'deki UavReports'a aktarıyoruz
                UavReports = request.UavSpecificReports
            };
        }

        private string BuildCsvContent(MissionReport report, Dictionary<string, UavStatsDetail>? uavReports)
        {
            var lines = new List<string>
            {
                "==================================================",
                "          SVT ÇOKLU İHA OPERASYON ANALİZ RAPORU   ",
                "==================================================",
                $"Oluşturulma Tarihi:,{DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                "",
                "--- [ BÖLÜM 1: GENEL GÖREV ÖZETİ (TÜMÜ) ] ---",
                $"Görev Başlangıcı:,{report.MissionStart:HH:mm:ss}",
                $"Görev Bitişi:,{report.MissionEnd:HH:mm:ss}",
                $"Toplam Tespit Sayısı:,{report.TotalDetections}",
                $"Toplam Tehdit Sayısı:,{report.ThreatCount}",
                $"Genel Ortalama Güven:,%{report.AverageConfidence:F1}",
                $"En Çok Tespit Edilen Nesne:,{report.MostDetectedType}",
                "",
                "--- [ BÖLÜM 2: İHA BAZLI AYRINTILI PERFORMANS ] ---",
                "UAV ID,UAV Adı,Tespit Sayısı,Tehdit Sayısı,Ort. Güven"
            };

            if (uavReports != null && uavReports.Count > 0)
            {
                foreach (var uav in uavReports)
                {
                    lines.Add($"{uav.Key},{uav.Value.UavName},{uav.Value.Detections},{uav.Value.Threats},%{uav.Value.AvgConf:F1}");
                }
            }
            else
            {
                lines.Add("Bireysel İHA verisi bulunamadı.,,,,");
            }

            lines.Add("");
            lines.Add("--- [ BÖLÜM 3: NESNE TİPİ DAĞILIM DETAYLARI ] ---");
            lines.Add("Nesne Tipi,Tespit Miktarı");

            if (report.TargetTypeCounts != null)
            {
                foreach (var kvp in report.TargetTypeCounts)
                {
                    lines.Add($"{kvp.Key.ToUpper()},{kvp.Value}");
                }
            }

            lines.Add("");
            lines.Add("--- [ BÖLÜM 4: TELEMETRİ VE SAHA LİMİTLERİ ] ---");
            lines.Add($"Maksimum İrtifa:,{report.MaxAltitude:F1} m");
            lines.Add($"Maksimum Hız:,{report.MaxSpeed:F1} km/h");
            lines.Add($"Minimum Batarya Seviyesi:,%{report.MinBattery:F0}");
            lines.Add($"Son Bilinen Konum (GPS):,{report.LastLatitude:F6} / {report.LastLongitude:F6}");

            lines.Add("");
            lines.Add("==================================================");
            lines.Add("          RAPORUN SONU - GÜVENLİ UÇUŞLAR          ");
            lines.Add("==================================================");

            return string.Join("\n", lines);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateThreshold(int value)
        {
            try
            {
                // TargetListenerService içinde statik bir broadcast metodu olduğunu varsayıyoruz
                // Bu metodu bir sonraki adımda yazacağız.
                await TargetListenerService.BroadcastThreshold(value);

                return Ok(new { message = $"Eşik {value} olarak İHA'lara iletildi." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // Modeller aynı kalıyor...
}

// --- MODELLER ---
public class MissionReportRequest
    {
        public Dictionary<string, UavStatsDetail>? UavSpecificReports { get; set; }
        public DateTime MissionStart { get; set; }
        public int TotalDetections { get; set; }
        public int ThreatCount { get; set; }
        public double AverageConfidence { get; set; }
        public Dictionary<string, int>? TargetTypeCounts { get; set; }
        public double MaxAltitude { get; set; }
        public double MaxSpeed { get; set; }
        public double MinBattery { get; set; }
        public double LastLatitude { get; set; }
        public double LastLongitude { get; set; }
        public List<ThreatEvent>? ThreatEvents { get; set; }
    }

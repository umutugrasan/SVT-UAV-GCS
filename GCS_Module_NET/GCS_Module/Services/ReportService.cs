using System;
using System.IO;
using System.Linq;
using GCS_Module.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCS_Module.Services
{
    public class ReportService
    {
        public byte[] GeneratePdfReport(MissionReport report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // ============================================================
                    // 1. HEADER (BAŞLIK)
                    // ============================================================
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Text("GÖREV ANALİZ RAPORU")
                            .FontSize(24).Bold().FontColor(Colors.Blue.Darken2);

                        headerCol.Item().Text($"Rapor Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .FontSize(12).FontColor(Colors.Grey.Darken1);

                        headerCol.Item().PaddingVertical(5).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                    });

                    // ============================================================
                    // 2. CONTENT (İÇERİK)
                    // ============================================================
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // GÖREV ÖZETİ (TÜMÜ)
                        col.Item().Background(Colors.Blue.Lighten5).Padding(15).Column(summary =>
                        {
                            summary.Item().Text("BÖLÜM 1: GENEL OPERASYON ÖZETİ (FİLO GENELİ)").FontSize(14).Bold();
                            summary.Item().PaddingTop(10).Text($"Başlangıç: {report.MissionStart:HH:mm:ss}  |  Bitiş: {report.MissionEnd:HH:mm:ss}  |  Süre: {report.Duration:hh\\:mm\\:ss}");
                        });

                        col.Item().Height(10);

                        // GENEL İSTATİSTİK TABLOSU
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(); cols.RelativeColumn();
                                cols.RelativeColumn(); cols.RelativeColumn();
                            });

                            table.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Toplam Tespit").FontColor(Colors.White).Bold().AlignCenter();
                            table.Cell().Background(Colors.Red.Darken2).Padding(8).Text("Tehdit Sayısı").FontColor(Colors.White).Bold().AlignCenter();
                            table.Cell().Background(Colors.Green.Darken2).Padding(8).Text("Ort. Güven").FontColor(Colors.White).Bold().AlignCenter();
                            table.Cell().Background(Colors.Orange.Darken2).Padding(8).Text("En Çok Tespit").FontColor(Colors.White).Bold().AlignCenter();

                            table.Cell().Border(1).Padding(10).Text($"{report.TotalDetections}").FontSize(16).Bold().AlignCenter();
                            table.Cell().Border(1).Padding(10).Text($"{report.ThreatCount}").FontSize(16).Bold().FontColor(Colors.Red.Darken2).AlignCenter();
                            table.Cell().Border(1).Padding(10).Text($"%{report.AverageConfidence:F1}").FontSize(16).Bold().AlignCenter();
                            table.Cell().Border(1).Padding(10).Text($"{report.MostDetectedType}").FontSize(12).Bold().AlignCenter();
                        });

                        col.Item().Height(20);

                        // ============================================================
                        // YENİ BÖLÜM: İHA BAZLI PERFORMANS ANALİZİ
                        // ============================================================
                        col.Item().Text("BÖLÜM 2: İHA BAZLI AYRINTILI ANALİZ").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2); // UAV Name
                                cols.RelativeColumn();  // Detections
                                cols.RelativeColumn();  // Threats
                                cols.RelativeColumn();  // Avg Confidence
                            });

                            // Tablo Başlıkları
                            table.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("İHA Adı").FontColor(Colors.White).Bold();
                            table.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Tespit").FontColor(Colors.White).Bold().AlignCenter();
                            table.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Tehdit").FontColor(Colors.White).Bold().AlignCenter();
                            table.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Güven").FontColor(Colors.White).Bold().AlignCenter();

                            // DİNAMİK İÇERİK: Modelden gelen Dictionary burada döngüye giriyor
                            if (report.UavReports != null && report.UavReports.Any())
                            {
                                foreach (var uav in report.UavReports)
                                {
                                    table.Cell().BorderBottom(1).Padding(5).Text(uav.Value.UavName);
                                    table.Cell().BorderBottom(1).Padding(5).Text($"{uav.Value.Detections}").AlignCenter();
                                    table.Cell().BorderBottom(1).Padding(5).Text($"{uav.Value.Threats}").FontColor(Colors.Red.Medium).AlignCenter();
                                    table.Cell().BorderBottom(1).Padding(5).Text($"%{uav.Value.AvgConf:F1}").AlignCenter();
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(4).Padding(10).Text("Bireysel İHA verisi kaydedilmedi.").Italic();
                            }
                        });

                        col.Item().Height(20);

                        // HEDEF DAĞILIMI VE TELEMETRİ (Mevcut kısımlar korunur)
                        col.Item().Row(row => {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("HEDEF TİPİ DAĞILIMI").FontSize(12).Bold();
                                c.Item().Table(t => {
                                    t.ColumnsDefinition(cs => { cs.RelativeColumn(); cs.RelativeColumn(); });
                                    foreach (var kvp in report.TargetTypeCounts.OrderByDescending(x => x.Value))
                                    {
                                        t.Cell().BorderBottom(1).Padding(2).Text(kvp.Key.ToUpper());
                                        t.Cell().BorderBottom(1).Padding(2).Text($"{kvp.Value}").AlignCenter();
                                    }
                                });
                            });

                            row.ConstantItem(20); // Spacer

                            row.RelativeItem().Column(c => {
                                c.Item().Text("TELEMETRİ LİMİTLERİ").FontSize(12).Bold();
                                c.Item().PaddingTop(5).Text($"📍 Maks. İrtifa: {report.MaxAltitude:F1} m");
                                c.Item().Text($"🚀 Maks. Hız: {report.MaxSpeed:F1} km/h");
                                c.Item().Text($"🔋 Min. Batarya: %{report.MinBattery:F0}");
                            });
                        });

                        // TEHDİT OLAYLARI (Take 5)
                        if (report.ThreatEvents != null && report.ThreatEvents.Any())
                        {
                            col.Item().Height(15);
                            col.Item().Text("KRİTİK TEHDİT KAYITLARI (SON 5)").FontSize(14).Bold().FontColor(Colors.Red.Darken2);
                            col.Item().Table(table => {
                                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(2); cols.RelativeColumn(); });
                                table.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Zaman").Bold();
                                table.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Konum").Bold();
                                table.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Yoğunluk").Bold().AlignCenter();
                                foreach (var threat in report.ThreatEvents.Take(5))
                                {
                                    table.Cell().BorderBottom(1).Padding(5).Text($"{threat.Time:HH:mm:ss}");
                                    table.Cell().BorderBottom(1).Padding(5).Text(threat.Location ?? "N/A");
                                    table.Cell().BorderBottom(1).Padding(5).Text($"{threat.VehicleCount}").AlignCenter();
                                }
                            });
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("SVT Yer Kontrol İstasyonu - Görev Raporu - Sayfa ");
                        text.CurrentPageNumber();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GCS_Module.Models
{
    /// <summary>
    /// Veritabanına kaydedilen hedef modeli
    /// </summary>
    public class TargetModel
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DbId { get; set; }

        // ============================================================
        // UAV BİLGİLERİ
        // ============================================================
        [MaxLength(20)]
        public string UavId { get; set; } = "UAV-001";

        [MaxLength(50)]
        public string UavName { get; set; } = "Alpha-1";

        // ============================================================
        // PYTHON'DAN GELEN ALANLAR
        // ============================================================

        [Required]
        public int TrackId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        // 👇👇👇 YENİ EKLENEN KISIM: PLAKA 👇👇👇
        [MaxLength(20)]
        public string Plate { get; set; } = "BELIRSIZ";

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string PixelCoords { get; set; } = "[0, 0]";

        [Column(TypeName = "nvarchar(50)")]
        public string? GpsCoords { get; set; }

        [Range(0.0, 1.0)]
        public double Confidence { get; set; }

        public bool IsThreat { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // ============================================================
        // TELEMETRY ALANLARI
        // ============================================================

        public double Battery { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }

        [MaxLength(20)]
        public string Signal { get; set; } = string.Empty;

        public double SignalStrength { get; set; }
        public double FlightTime { get; set; }

        // ============================================================
        // COMPUTED PROPERTIES (DB'ye kaydedilmez)
        // ============================================================

        [NotMapped]
        public int[] PixelCoordsArray
        {
            get
            {
                if (string.IsNullOrEmpty(PixelCoords)) return new int[] { 0, 0 };
                try
                {
                    var cleaned = PixelCoords.Trim('[', ']');
                    var parts = cleaned.Split(',');
                    return new int[] { int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()) };
                }
                catch { return new int[] { 0, 0 }; }
            }
        }

        [NotMapped]
        public double[] GpsCoordsArray
        {
            get
            {
                if (string.IsNullOrEmpty(GpsCoords)) return new double[] { 0.0, 0.0 };
                try
                {
                    var cleaned = GpsCoords.Trim('[', ']');
                    var parts = cleaned.Split(',');
                    return new double[] { double.Parse(parts[0].Trim()), double.Parse(parts[1].Trim()) };
                }
                catch { return new double[] { 0.0, 0.0 }; }
            }
        }

        [NotMapped]
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        [NotMapped]
        public string FormattedCoords => $"X:{PixelCoordsArray[0]}, Y:{PixelCoordsArray[1]}";

        [NotMapped]
        public string FormattedGps => string.IsNullOrEmpty(GpsCoords) ? "N/A" : $"{GpsCoordsArray[0]:F6}, {GpsCoordsArray[1]:F6}";

        [NotMapped]
        public string FormattedTelemetry => $"🔋{Battery:F0}% | 📍{Altitude:F1}m | 🚀{Speed:F1}km/h | 🧭{Heading:F0}°";

        [NotMapped]
        public string ThreatStatus => IsThreat ? "⚠️ THREAT" : "✅ CLEAR";

        [NotMapped]
        public string BatteryStatus
        {
            get
            {
                if (Battery >= 50) return "Normal";
                if (Battery >= 25) return "Low";
                if (Battery >= 10) return "Critical";
                return "Emergency";
            }
        }

        [NotMapped]
        public string UavInfo => $"[{UavId}] {UavName}";
    }

    // ============================================================
    // JSON DESERIALIZE YARDIMCI SINIFLAR
    // ============================================================

    public class TelemetryData
    {
        [JsonPropertyName("battery")]
        public double Battery { get; set; }

        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("heading")]
        public double Heading { get; set; }

        [JsonPropertyName("signal")]
        public string Signal { get; set; } = string.Empty;

        [JsonPropertyName("signalStrength")]
        public double SignalStrength { get; set; }

        [JsonPropertyName("isFlying")]
        public bool IsFlying { get; set; }

        [JsonPropertyName("flightTime")]
        public double FlightTime { get; set; }
    }

    /// <summary>
    /// Python'dan gelen ham JSON yapısı
    /// </summary>
    public class RawTargetData
    {
        [JsonPropertyName("uavId")]
        public string UavId { get; set; } = "UAV-001";

        [JsonPropertyName("uavName")]
        public string UavName { get; set; } = "Alpha-1";

        [JsonPropertyName("trackId")]
        public int TrackId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        // 👇👇👇 YENİ EKLENEN KISIM: PLAKA (JSON'dan Okuma) 👇👇👇
        [JsonPropertyName("plate")]
        public string? Plate { get; set; }

        [JsonPropertyName("pixelCoords")]
        public string PixelCoords { get; set; } = "[0, 0]";

        [JsonPropertyName("gpsCoords")]
        public string GpsCoords { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("isThreat")]
        public bool IsThreat { get; set; }

        [JsonPropertyName("telemetry")]
        public TelemetryData? Telemetry { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>
        /// RawTargetData → TargetModel dönüşümü
        /// </summary>
        public TargetModel ToTargetModel()
        {
            DateTime ts;
            if (!DateTime.TryParse(this.Timestamp, out ts))
            {
                ts = DateTime.Now;
            }

            return new TargetModel
            {
                UavId = this.UavId,
                UavName = this.UavName,
                TrackId = this.TrackId,
                Type = this.Type,
                // 👇👇👇 EŞLEŞTİRME BURADA 👇👇👇
                Plate = this.Plate ?? "BELIRSIZ",
                PixelCoords = this.PixelCoords,
                GpsCoords = this.GpsCoords,
                Confidence = this.Confidence,
                IsThreat = this.IsThreat,
                Timestamp = ts,
                Battery = this.Telemetry?.Battery ?? 0,
                Altitude = this.Telemetry?.Altitude ?? 0,
                Speed = this.Telemetry?.Speed ?? 0,
                Heading = this.Telemetry?.Heading ?? 0,
                Signal = this.Telemetry?.Signal ?? "Unknown",
                SignalStrength = this.Telemetry?.SignalStrength ?? 0,
                FlightTime = this.Telemetry?.FlightTime ?? 0
            };
        }
    }

    public class TargetBatch
    {
        [JsonPropertyName("targets")]
        public List<RawTargetData> Targets { get; set; } = new List<RawTargetData>();

        public List<TargetModel> ToTargetModels()
        {
            var models = new List<TargetModel>();
            foreach (var raw in Targets)
            {
                models.Add(raw.ToTargetModel());
            }
            return models;
        }
    }
}
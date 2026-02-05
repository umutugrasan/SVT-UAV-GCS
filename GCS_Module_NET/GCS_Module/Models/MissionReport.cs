using System;
using System.Collections.Generic;

namespace GCS_Module.Models
{
    public class MissionReport
    {
        public DateTime MissionStart { get; set; }
        public DateTime MissionEnd { get; set; }
        public TimeSpan Duration => MissionEnd - MissionStart;

        public int TotalDetections { get; set; }
        public int ThreatCount { get; set; }
        public double AverageConfidence { get; set; }

        public Dictionary<string, int> TargetTypeCounts { get; set; } = new();
        public string MostDetectedType { get; set; } = string.Empty;

        // Telemetri özeti
        public double MaxAltitude { get; set; }
        public double MaxSpeed { get; set; }
        public double MinBattery { get; set; }

        // Son konum
        public double LastLatitude { get; set; }
        public double LastLongitude { get; set; }

        public List<ThreatEvent> ThreatEvents { get; set; } = new();

        // ============================================================
        // YENİ: ÇOKLU İHA DETAYLARI (PDF ve EXCEL BURAYI OKUYACAK)
        // ============================================================
        public Dictionary<string, UavStatsDetail>? UavReports { get; set; } = new();
    }

    public class UavStatsDetail
    {
        public string UavName { get; set; } = string.Empty;
        public int Detections { get; set; }
        public int Threats { get; set; }
        public double AvgConf { get; set; }
    }

    public class ThreatEvent
    {
        public DateTime Time { get; set; }
        public string Location { get; set; } = string.Empty;
        public int VehicleCount { get; set; }
    }
}
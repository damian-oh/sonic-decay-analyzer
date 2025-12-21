using SQLite;

namespace SonicDecay.App.Models
{
    [Table("MeasurementLogs")]
    public class MeasurementLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int BaselineId { get; set; } // Foreign Key for StringBaseline

        // Measured Values
        public double CurrentCentroid { get; set; }
        public double CurrentHighRatio { get; set; }

        // Calculated Logic
        public double DecayPercentage { get; set; }
        public double PlayTimeHours { get; set; } // User-input play hours since last measurement

        public string? Note { get; set; }

        public DateTime MeasuredAt { get; set; } = DateTime.Now;
    }
}
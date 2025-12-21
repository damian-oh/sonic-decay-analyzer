using SQLite;

namespace SonicDecay.App.Models
{
    [Table("StringBaselines")]
    public class StringBaseline
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SetId { get; set; } // Foreign Key for StringSet

        public int StringNumber { get; set; } // 1 ~ 6

        public double FundamentalFreq { get; set; } // e.g., 82.41 for Low E

        // Reference Metrics (Physical Fingerprint)
        public double InitialCentroid { get; set; }
        public double InitialHighRatio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
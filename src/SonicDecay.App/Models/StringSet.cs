using SQLite;

namespace SonicDecay.App.Models
{
    [Table("StringSets")]
    public class StringSet
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Brand { get; set; } = string.Empty; // e.g., Elixir

        [NotNull]
        public string Model { get; set; } = string.Empty; // e.g., Nanoweb

        // Gauges for all 6 strings in the set
        public double GaugeE1 { get; set; }
        public double GaugeB2 { get; set; }
        public double GaugeG3 { get; set; }
        public double GaugeD4 { get; set; }
        public double GaugeA5 { get; set; }
        public double GaugeE6 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

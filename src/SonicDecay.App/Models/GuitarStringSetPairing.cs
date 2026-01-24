using SQLite;

namespace SonicDecay.App.Models
{
    /// <summary>
    /// Junction table representing the relationship between guitars and string sets.
    /// Tracks when strings were installed and removed from a guitar, enabling
    /// historical analysis of string usage patterns.
    /// </summary>
    [Table("GuitarStringSetPairings")]
    public class GuitarStringSetPairing
    {
        /// <summary>
        /// Gets or sets the unique identifier for the pairing.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the Guitar entity.
        /// </summary>
        [Indexed]
        public int GuitarId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the StringSet entity.
        /// </summary>
        [Indexed]
        public int SetId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the strings were installed on the guitar.
        /// </summary>
        public DateTime InstalledAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the timestamp when the strings were removed.
        /// Null indicates the strings are currently installed.
        /// </summary>
        public DateTime? RemovedAt { get; set; }

        /// <summary>
        /// Gets or sets whether this pairing is currently active.
        /// Only one pairing per guitar can be active at any time.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets optional notes about this string installation.
        /// </summary>
        public string? Notes { get; set; }
    }
}

using SQLite;

namespace SonicDecay.App.Models
{
    /// <summary>
    /// Represents a guitar instrument in the user's collection.
    /// Guitars can be paired with string sets to track string installation history.
    /// </summary>
    [Table("Guitars")]
    public class Guitar
    {
        /// <summary>
        /// Gets or sets the unique identifier for the guitar.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user-defined name for the guitar (e.g., "My Strat", "Studio Les Paul").
        /// </summary>
        [NotNull]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manufacturer/brand (e.g., "Fender", "Gibson", "PRS").
        /// </summary>
        public string? Make { get; set; }

        /// <summary>
        /// Gets or sets the model name (e.g., "Stratocaster", "Les Paul Standard").
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the guitar type. Valid values: "Electric", "Acoustic", "Classical", "Bass".
        /// </summary>
        [NotNull]
        public string Type { get; set; } = "Electric";

        /// <summary>
        /// Gets or sets optional notes about the guitar.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the guitar was added to the system.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

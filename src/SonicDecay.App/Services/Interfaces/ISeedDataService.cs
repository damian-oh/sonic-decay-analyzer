namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Service for seeding the database with preset string set data.
    /// Populates the database with common brand/model combinations on first run.
    /// </summary>
    public interface ISeedDataService
    {
        /// <summary>
        /// Seeds the database with preset string sets if no data exists.
        /// Should be called during application initialization.
        /// </summary>
        /// <returns>A task representing the async seeding operation.</returns>
        Task SeedIfEmptyAsync();

        /// <summary>
        /// Forces re-seeding of preset data, removing existing presets first.
        /// User-created custom string sets are preserved.
        /// </summary>
        /// <returns>A task representing the async seeding operation.</returns>
        Task ReseedPresetsAsync();

        /// <summary>
        /// Gets a value indicating whether seed data has been applied.
        /// </summary>
        bool IsSeeded { get; }
    }
}

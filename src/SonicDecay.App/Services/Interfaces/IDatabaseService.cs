using SQLite;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Provides thread-safe SQLite database access as a singleton service.
    /// Manages connection lifecycle and table initialization.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Gets the async SQLite connection instance.
        /// </summary>
        SQLiteAsyncConnection Connection { get; }

        /// <summary>
        /// Initializes the database by creating tables if they don't exist.
        /// Must be called before any repository operations.
        /// </summary>
        /// <returns>A task representing the async initialization operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Gets a value indicating whether the database has been initialized.
        /// </summary>
        bool IsInitialized { get; }
    }
}

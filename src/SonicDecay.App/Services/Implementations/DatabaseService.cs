using SQLite;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Thread-safe SQLite database service implementing the singleton pattern.
    /// Manages connection lifecycle and ensures tables are created on first access.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private const string DatabaseFilename = "SonicDecay.db";

        private readonly SQLiteAsyncConnection _connection;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private volatile bool _isInitialized;

        /// <inheritdoc />
        public SQLiteAsyncConnection Connection => _connection;

        /// <inheritdoc />
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// Creates the SQLite connection with the database file in the app data folder.
        /// </summary>
        public DatabaseService()
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

            _connection = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite |
                                                                   SQLiteOpenFlags.Create |
                                                                   SQLiteOpenFlags.SharedCache);
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                // Create tables in dependency order
                await _connection.CreateTableAsync<Guitar>();
                await _connection.CreateTableAsync<StringSet>();
                await _connection.CreateTableAsync<GuitarStringSetPairing>();
                await _connection.CreateTableAsync<StringBaseline>();
                await _connection.CreateTableAsync<MeasurementLog>();

                _isInitialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }
    }
}

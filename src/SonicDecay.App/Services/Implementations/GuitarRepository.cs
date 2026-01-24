using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Repository implementation for Guitar entity CRUD operations.
    /// Implements manual cascade delete to GuitarStringSetPairings.
    /// </summary>
    public class GuitarRepository : IGuitarRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly IGuitarStringSetPairingRepository _pairingRepository;

        /// <summary>
        /// Initializes a new instance of the GuitarRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for connection access.</param>
        /// <param name="pairingRepository">The pairing repository for cascade operations.</param>
        public GuitarRepository(
            IDatabaseService databaseService,
            IGuitarStringSetPairingRepository pairingRepository)
        {
            _databaseService = databaseService;
            _pairingRepository = pairingRepository;
        }

        /// <inheritdoc />
        public async Task<List<Guitar>> GetAllAsync()
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<Guitar>()
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Guitar?> GetByIdAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<Guitar>()
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(Guitar guitar)
        {
            await _databaseService.InitializeAsync();
            guitar.CreatedAt = DateTime.Now;
            return await _databaseService.Connection.InsertAsync(guitar);
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(Guitar guitar)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection.UpdateAsync(guitar);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int id)
        {
            await _databaseService.InitializeAsync();

            // Manual cascade delete: first delete all related pairings
            await _pairingRepository.DeleteByGuitarIdAsync(id);

            // Then delete the guitar
            return await _databaseService.Connection
                .DeleteAsync<Guitar>(id);
        }

        /// <inheritdoc />
        public async Task<List<Guitar>> GetByTypeAsync(string type)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<Guitar>()
                .Where(g => g.Type == type)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }
    }
}

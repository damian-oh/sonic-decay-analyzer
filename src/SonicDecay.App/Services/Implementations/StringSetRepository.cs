using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Repository implementation for StringSet entity CRUD operations.
    /// Implements manual cascade delete to StringBaselines, MeasurementLogs, and GuitarStringSetPairings.
    /// </summary>
    public class StringSetRepository : IStringSetRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly IGuitarStringSetPairingRepository _pairingRepository;

        /// <summary>
        /// Initializes a new instance of the StringSetRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for connection access.</param>
        /// <param name="baselineRepository">The baseline repository for cascade operations.</param>
        /// <param name="pairingRepository">The pairing repository for cascade operations.</param>
        public StringSetRepository(
            IDatabaseService databaseService,
            IStringBaselineRepository baselineRepository,
            IGuitarStringSetPairingRepository pairingRepository)
        {
            _databaseService = databaseService;
            _baselineRepository = baselineRepository;
            _pairingRepository = pairingRepository;
        }

        /// <inheritdoc />
        public async Task<List<StringSet>> GetAllAsync()
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<StringSet>()
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<StringSet?> GetByIdAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<StringSet>()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(StringSet stringSet)
        {
            await _databaseService.InitializeAsync();
            stringSet.CreatedAt = DateTime.Now;
            return await _databaseService.Connection.InsertAsync(stringSet);
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(StringSet stringSet)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection.UpdateAsync(stringSet);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int id)
        {
            await _databaseService.InitializeAsync();

            // Manual cascade delete: first delete all related baselines
            // (which will cascade delete their measurement logs)
            await _baselineRepository.DeleteBySetIdAsync(id);

            // Delete all related guitar pairings
            await _pairingRepository.DeleteBySetIdAsync(id);

            // Then delete the string set
            return await _databaseService.Connection
                .DeleteAsync<StringSet>(id);
        }
    }
}

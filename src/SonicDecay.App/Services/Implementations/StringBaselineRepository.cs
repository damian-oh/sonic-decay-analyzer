using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Repository implementation for StringBaseline entity CRUD operations.
    /// Implements manual cascade delete to MeasurementLogs.
    /// </summary>
    public class StringBaselineRepository : IStringBaselineRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly IMeasurementLogRepository _measurementLogRepository;

        /// <summary>
        /// Initializes a new instance of the StringBaselineRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for connection access.</param>
        /// <param name="measurementLogRepository">The measurement log repository for cascade operations.</param>
        public StringBaselineRepository(
            IDatabaseService databaseService,
            IMeasurementLogRepository measurementLogRepository)
        {
            _databaseService = databaseService;
            _measurementLogRepository = measurementLogRepository;
        }

        /// <inheritdoc />
        public async Task<List<StringBaseline>> GetBySetIdAsync(int setId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<StringBaseline>()
                .Where(b => b.SetId == setId)
                .OrderBy(b => b.StringNumber)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<StringBaseline?> GetByIdAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<StringBaseline>()
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <inheritdoc />
        public async Task<StringBaseline?> GetBySetIdAndStringNumberAsync(int setId, int stringNumber)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<StringBaseline>()
                .FirstOrDefaultAsync(b => b.SetId == setId && b.StringNumber == stringNumber);
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(StringBaseline baseline)
        {
            await _databaseService.InitializeAsync();
            baseline.CreatedAt = DateTime.Now;
            return await _databaseService.Connection.InsertAsync(baseline);
        }

        /// <inheritdoc />
        public async Task<int> CreateBatchAsync(IEnumerable<StringBaseline> baselines)
        {
            await _databaseService.InitializeAsync();

            var now = DateTime.Now;
            var baselineList = baselines.ToList();

            foreach (var baseline in baselineList)
            {
                baseline.CreatedAt = now;
            }

            return await _databaseService.Connection.InsertAllAsync(baselineList);
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(StringBaseline baseline)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection.UpdateAsync(baseline);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int id)
        {
            await _databaseService.InitializeAsync();

            // Manual cascade delete: first delete all related measurement logs
            await _measurementLogRepository.DeleteByBaselineIdAsync(id);

            // Then delete the baseline
            return await _databaseService.Connection
                .DeleteAsync<StringBaseline>(id);
        }

        /// <inheritdoc />
        public async Task<int> DeleteBySetIdAsync(int setId)
        {
            await _databaseService.InitializeAsync();

            // Batch delete measurement logs for all baselines in this set
            // Uses subquery for O(1) instead of O(N) queries
            await _databaseService.Connection.ExecuteAsync(
                "DELETE FROM MeasurementLogs WHERE BaselineId IN (SELECT Id FROM StringBaselines WHERE SetId = ?)",
                setId);

            // Batch delete all baselines for this set
            return await _databaseService.Connection.ExecuteAsync(
                "DELETE FROM StringBaselines WHERE SetId = ?",
                setId);
        }
    }
}

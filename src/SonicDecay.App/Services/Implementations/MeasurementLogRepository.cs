using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Repository implementation for MeasurementLog entity CRUD operations.
    /// Stores time-series spectral decay data for string baselines.
    /// </summary>
    public class MeasurementLogRepository : IMeasurementLogRepository
    {
        private readonly IDatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the MeasurementLogRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for connection access.</param>
        public MeasurementLogRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <inheritdoc />
        public async Task<List<MeasurementLog>> GetByBaselineIdAsync(int baselineId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<MeasurementLog>()
                .Where(m => m.BaselineId == baselineId)
                .OrderByDescending(m => m.MeasuredAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<MeasurementLog?> GetByIdAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<MeasurementLog>()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <inheritdoc />
        public async Task<MeasurementLog?> GetLatestByBaselineIdAsync(int baselineId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<MeasurementLog>()
                .Where(m => m.BaselineId == baselineId)
                .OrderByDescending(m => m.MeasuredAt)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(MeasurementLog log)
        {
            await _databaseService.InitializeAsync();
            log.MeasuredAt = DateTime.Now;
            return await _databaseService.Connection.InsertAsync(log);
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(MeasurementLog log)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection.UpdateAsync(log);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .DeleteAsync<MeasurementLog>(id);
        }

        /// <inheritdoc />
        public async Task<int> DeleteByBaselineIdAsync(int baselineId)
        {
            await _databaseService.InitializeAsync();

            // Get all logs for this baseline
            var logs = await GetByBaselineIdAsync(baselineId);

            // Delete each log
            var deleteCount = 0;
            foreach (var log in logs)
            {
                deleteCount += await _databaseService.Connection
                    .DeleteAsync<MeasurementLog>(log.Id);
            }

            return deleteCount;
        }

        /// <inheritdoc />
        public async Task<int> GetCountByBaselineIdAsync(int baselineId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<MeasurementLog>()
                .Where(m => m.BaselineId == baselineId)
                .CountAsync();
        }
    }
}

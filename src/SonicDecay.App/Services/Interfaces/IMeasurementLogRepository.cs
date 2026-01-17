using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Repository interface for MeasurementLog entity CRUD operations.
    /// Stores time-series decay data linked to StringBaseline entities.
    /// </summary>
    public interface IMeasurementLogRepository
    {
        /// <summary>
        /// Retrieves all measurement logs for a specific baseline.
        /// Results are ordered by MeasuredAt descending (most recent first).
        /// </summary>
        /// <param name="baselineId">The parent StringBaseline ID.</param>
        /// <returns>A list of MeasurementLog entities for the baseline.</returns>
        Task<List<MeasurementLog>> GetByBaselineIdAsync(int baselineId);

        /// <summary>
        /// Retrieves a measurement log by its unique identifier.
        /// </summary>
        /// <param name="id">The MeasurementLog ID.</param>
        /// <returns>The MeasurementLog if found; otherwise, null.</returns>
        Task<MeasurementLog?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves the most recent measurement log for a specific baseline.
        /// </summary>
        /// <param name="baselineId">The parent StringBaseline ID.</param>
        /// <returns>The most recent MeasurementLog if any exist; otherwise, null.</returns>
        Task<MeasurementLog?> GetLatestByBaselineIdAsync(int baselineId);

        /// <summary>
        /// Creates a new measurement log in the database.
        /// </summary>
        /// <param name="log">The MeasurementLog entity to create.</param>
        /// <returns>The number of rows inserted (1 on success).</returns>
        Task<int> CreateAsync(MeasurementLog log);

        /// <summary>
        /// Updates an existing measurement log.
        /// </summary>
        /// <param name="log">The MeasurementLog entity with updated values.</param>
        /// <returns>The number of rows updated (1 on success).</returns>
        Task<int> UpdateAsync(MeasurementLog log);

        /// <summary>
        /// Deletes a measurement log.
        /// </summary>
        /// <param name="id">The MeasurementLog ID to delete.</param>
        /// <returns>The number of rows deleted (1 on success).</returns>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Deletes all measurement logs for a specific baseline.
        /// Used during cascade delete from StringBaseline.
        /// </summary>
        /// <param name="baselineId">The parent StringBaseline ID.</param>
        /// <returns>The number of MeasurementLog rows deleted.</returns>
        Task<int> DeleteByBaselineIdAsync(int baselineId);

        /// <summary>
        /// Gets the total count of measurements for a specific baseline.
        /// Useful for statistics and history displays.
        /// </summary>
        /// <param name="baselineId">The parent StringBaseline ID.</param>
        /// <returns>The count of measurement logs.</returns>
        Task<int> GetCountByBaselineIdAsync(int baselineId);
    }
}

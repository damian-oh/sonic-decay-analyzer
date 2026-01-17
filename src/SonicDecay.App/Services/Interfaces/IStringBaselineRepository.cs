using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Repository interface for StringBaseline entity CRUD operations.
    /// Each StringSet has exactly 6 baselines (one per guitar string).
    /// Deletion cascades to all related MeasurementLogs.
    /// </summary>
    public interface IStringBaselineRepository
    {
        /// <summary>
        /// Retrieves all baselines for a specific string set.
        /// </summary>
        /// <param name="setId">The parent StringSet ID.</param>
        /// <returns>A list of StringBaseline entities for the set (typically 6).</returns>
        Task<List<StringBaseline>> GetBySetIdAsync(int setId);

        /// <summary>
        /// Retrieves a baseline by its unique identifier.
        /// </summary>
        /// <param name="id">The StringBaseline ID.</param>
        /// <returns>The StringBaseline if found; otherwise, null.</returns>
        Task<StringBaseline?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves a baseline for a specific string number within a set.
        /// </summary>
        /// <param name="setId">The parent StringSet ID.</param>
        /// <param name="stringNumber">The string number (1-6, where 1=High E, 6=Low E).</param>
        /// <returns>The StringBaseline if found; otherwise, null.</returns>
        Task<StringBaseline?> GetBySetIdAndStringNumberAsync(int setId, int stringNumber);

        /// <summary>
        /// Creates a new baseline in the database.
        /// </summary>
        /// <param name="baseline">The StringBaseline entity to create.</param>
        /// <returns>The number of rows inserted (1 on success).</returns>
        Task<int> CreateAsync(StringBaseline baseline);

        /// <summary>
        /// Creates multiple baselines in a single transaction.
        /// Typically used to create all 6 baselines for a new string set.
        /// </summary>
        /// <param name="baselines">The list of StringBaseline entities to create.</param>
        /// <returns>The total number of rows inserted.</returns>
        Task<int> CreateBatchAsync(IEnumerable<StringBaseline> baselines);

        /// <summary>
        /// Updates an existing baseline.
        /// </summary>
        /// <param name="baseline">The StringBaseline entity with updated values.</param>
        /// <returns>The number of rows updated (1 on success).</returns>
        Task<int> UpdateAsync(StringBaseline baseline);

        /// <summary>
        /// Deletes a baseline and all related MeasurementLogs.
        /// Implements manual cascade delete per architectural requirements.
        /// </summary>
        /// <param name="id">The StringBaseline ID to delete.</param>
        /// <returns>The number of StringBaseline rows deleted (1 on success).</returns>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Deletes all baselines for a specific string set.
        /// Used during cascade delete from StringSet.
        /// </summary>
        /// <param name="setId">The parent StringSet ID.</param>
        /// <returns>The number of StringBaseline rows deleted.</returns>
        Task<int> DeleteBySetIdAsync(int setId);
    }
}

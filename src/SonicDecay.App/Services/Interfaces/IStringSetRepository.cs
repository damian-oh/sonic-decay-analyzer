using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Repository interface for StringSet entity CRUD operations.
    /// Deletion cascades to all related StringBaselines and their MeasurementLogs.
    /// </summary>
    public interface IStringSetRepository
    {
        /// <summary>
        /// Retrieves all string sets from the database.
        /// </summary>
        /// <returns>A list of all StringSet entities.</returns>
        Task<List<StringSet>> GetAllAsync();

        /// <summary>
        /// Retrieves a string set by its unique identifier.
        /// </summary>
        /// <param name="id">The StringSet ID.</param>
        /// <returns>The StringSet if found; otherwise, null.</returns>
        Task<StringSet?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new string set in the database.
        /// </summary>
        /// <param name="stringSet">The StringSet entity to create.</param>
        /// <returns>The number of rows inserted (1 on success).</returns>
        Task<int> CreateAsync(StringSet stringSet);

        /// <summary>
        /// Updates an existing string set.
        /// </summary>
        /// <param name="stringSet">The StringSet entity with updated values.</param>
        /// <returns>The number of rows updated (1 on success).</returns>
        Task<int> UpdateAsync(StringSet stringSet);

        /// <summary>
        /// Deletes a string set and all related StringBaselines and MeasurementLogs.
        /// Implements manual cascade delete per architectural requirements.
        /// </summary>
        /// <param name="id">The StringSet ID to delete.</param>
        /// <returns>The number of StringSet rows deleted (1 on success).</returns>
        Task<int> DeleteAsync(int id);
    }
}

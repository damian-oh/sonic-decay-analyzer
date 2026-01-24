using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Repository interface for Guitar entity CRUD operations.
    /// Deletion cascades to all related GuitarStringSetPairings.
    /// </summary>
    public interface IGuitarRepository
    {
        /// <summary>
        /// Retrieves all guitars from the database.
        /// </summary>
        /// <returns>A list of all Guitar entities ordered by name.</returns>
        Task<List<Guitar>> GetAllAsync();

        /// <summary>
        /// Retrieves a guitar by its unique identifier.
        /// </summary>
        /// <param name="id">The Guitar ID.</param>
        /// <returns>The Guitar if found; otherwise, null.</returns>
        Task<Guitar?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new guitar in the database.
        /// </summary>
        /// <param name="guitar">The Guitar entity to create.</param>
        /// <returns>The number of rows inserted (1 on success).</returns>
        Task<int> CreateAsync(Guitar guitar);

        /// <summary>
        /// Updates an existing guitar.
        /// </summary>
        /// <param name="guitar">The Guitar entity with updated values.</param>
        /// <returns>The number of rows updated (1 on success).</returns>
        Task<int> UpdateAsync(Guitar guitar);

        /// <summary>
        /// Deletes a guitar and all related GuitarStringSetPairings.
        /// Implements manual cascade delete per architectural requirements.
        /// </summary>
        /// <param name="id">The Guitar ID to delete.</param>
        /// <returns>The number of Guitar rows deleted (1 on success).</returns>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Retrieves all guitars of a specific type.
        /// </summary>
        /// <param name="type">The guitar type (e.g., "Electric", "Acoustic").</param>
        /// <returns>A list of guitars matching the specified type.</returns>
        Task<List<Guitar>> GetByTypeAsync(string type);
    }
}

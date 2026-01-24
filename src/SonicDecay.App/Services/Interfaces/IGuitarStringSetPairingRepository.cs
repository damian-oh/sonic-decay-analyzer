using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Repository interface for GuitarStringSetPairing junction table operations.
    /// Manages the relationship between guitars and string sets, enforcing
    /// the constraint that only one pairing per guitar can be active.
    /// </summary>
    public interface IGuitarStringSetPairingRepository
    {
        /// <summary>
        /// Retrieves all pairings from the database.
        /// </summary>
        /// <returns>A list of all GuitarStringSetPairing entities.</returns>
        Task<List<GuitarStringSetPairing>> GetAllAsync();

        /// <summary>
        /// Retrieves a pairing by its unique identifier.
        /// </summary>
        /// <param name="id">The pairing ID.</param>
        /// <returns>The pairing if found; otherwise, null.</returns>
        Task<GuitarStringSetPairing?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all pairings for a specific guitar.
        /// </summary>
        /// <param name="guitarId">The Guitar ID.</param>
        /// <returns>A list of pairings ordered by InstalledAt descending.</returns>
        Task<List<GuitarStringSetPairing>> GetByGuitarIdAsync(int guitarId);

        /// <summary>
        /// Retrieves all pairings for a specific string set.
        /// </summary>
        /// <param name="setId">The StringSet ID.</param>
        /// <returns>A list of pairings ordered by InstalledAt descending.</returns>
        Task<List<GuitarStringSetPairing>> GetBySetIdAsync(int setId);

        /// <summary>
        /// Retrieves the currently active pairing for a guitar.
        /// </summary>
        /// <param name="guitarId">The Guitar ID.</param>
        /// <returns>The active pairing if found; otherwise, null.</returns>
        Task<GuitarStringSetPairing?> GetActiveByGuitarIdAsync(int guitarId);

        /// <summary>
        /// Creates a new pairing and optionally sets it as active.
        /// If IsActive is true, deactivates any existing active pairing for the guitar.
        /// </summary>
        /// <param name="pairing">The pairing to create.</param>
        /// <returns>The number of rows inserted (1 on success).</returns>
        Task<int> CreateAsync(GuitarStringSetPairing pairing);

        /// <summary>
        /// Updates an existing pairing.
        /// If IsActive is being set to true, deactivates any other active pairing for the guitar.
        /// </summary>
        /// <param name="pairing">The pairing with updated values.</param>
        /// <returns>The number of rows updated (1 on success).</returns>
        Task<int> UpdateAsync(GuitarStringSetPairing pairing);

        /// <summary>
        /// Deletes a pairing.
        /// </summary>
        /// <param name="id">The pairing ID to delete.</param>
        /// <returns>The number of rows deleted (1 on success).</returns>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Deletes all pairings for a specific guitar.
        /// Used for cascade delete when a guitar is removed.
        /// </summary>
        /// <param name="guitarId">The Guitar ID.</param>
        /// <returns>The number of pairings deleted.</returns>
        Task<int> DeleteByGuitarIdAsync(int guitarId);

        /// <summary>
        /// Deletes all pairings for a specific string set.
        /// Used for cascade delete when a string set is removed.
        /// </summary>
        /// <param name="setId">The StringSet ID.</param>
        /// <returns>The number of pairings deleted.</returns>
        Task<int> DeleteBySetIdAsync(int setId);

        /// <summary>
        /// Deactivates the currently active pairing for a guitar and sets a new pairing as active.
        /// Sets RemovedAt timestamp on the previous active pairing.
        /// </summary>
        /// <param name="guitarId">The Guitar ID.</param>
        /// <param name="newPairingId">The ID of the pairing to activate.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        Task<bool> ActivatePairingAsync(int guitarId, int newPairingId);
    }
}

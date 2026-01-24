using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Repository implementation for GuitarStringSetPairing junction table operations.
    /// Enforces the constraint that only one pairing per guitar can be active.
    /// </summary>
    public class GuitarStringSetPairingRepository : IGuitarStringSetPairingRepository
    {
        private readonly IDatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the GuitarStringSetPairingRepository class.
        /// </summary>
        /// <param name="databaseService">The database service for connection access.</param>
        public GuitarStringSetPairingRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <inheritdoc />
        public async Task<List<GuitarStringSetPairing>> GetAllAsync()
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<GuitarStringSetPairing>()
                .OrderByDescending(p => p.InstalledAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<GuitarStringSetPairing?> GetByIdAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<GuitarStringSetPairing>()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<GuitarStringSetPairing>> GetByGuitarIdAsync(int guitarId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<GuitarStringSetPairing>()
                .Where(p => p.GuitarId == guitarId)
                .OrderByDescending(p => p.InstalledAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<GuitarStringSetPairing>> GetBySetIdAsync(int setId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<GuitarStringSetPairing>()
                .Where(p => p.SetId == setId)
                .OrderByDescending(p => p.InstalledAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<GuitarStringSetPairing?> GetActiveByGuitarIdAsync(int guitarId)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .Table<GuitarStringSetPairing>()
                .Where(p => p.GuitarId == guitarId && p.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(GuitarStringSetPairing pairing)
        {
            await _databaseService.InitializeAsync();

            // If this pairing is active, deactivate any existing active pairing
            if (pairing.IsActive)
            {
                await DeactivateCurrentAsync(pairing.GuitarId);
            }

            pairing.InstalledAt = DateTime.Now;
            return await _databaseService.Connection.InsertAsync(pairing);
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(GuitarStringSetPairing pairing)
        {
            await _databaseService.InitializeAsync();

            // If activating this pairing, deactivate any other active pairing
            if (pairing.IsActive)
            {
                var currentActive = await GetActiveByGuitarIdAsync(pairing.GuitarId);
                if (currentActive != null && currentActive.Id != pairing.Id)
                {
                    currentActive.IsActive = false;
                    currentActive.RemovedAt = DateTime.Now;
                    await _databaseService.Connection.UpdateAsync(currentActive);
                }
            }

            return await _databaseService.Connection.UpdateAsync(pairing);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int id)
        {
            await _databaseService.InitializeAsync();
            return await _databaseService.Connection
                .DeleteAsync<GuitarStringSetPairing>(id);
        }

        /// <inheritdoc />
        public async Task<int> DeleteByGuitarIdAsync(int guitarId)
        {
            await _databaseService.InitializeAsync();
            var pairings = await GetByGuitarIdAsync(guitarId);
            var count = 0;

            foreach (var pairing in pairings)
            {
                count += await _databaseService.Connection
                    .DeleteAsync<GuitarStringSetPairing>(pairing.Id);
            }

            return count;
        }

        /// <inheritdoc />
        public async Task<int> DeleteBySetIdAsync(int setId)
        {
            await _databaseService.InitializeAsync();
            var pairings = await GetBySetIdAsync(setId);
            var count = 0;

            foreach (var pairing in pairings)
            {
                count += await _databaseService.Connection
                    .DeleteAsync<GuitarStringSetPairing>(pairing.Id);
            }

            return count;
        }

        /// <inheritdoc />
        public async Task<bool> ActivatePairingAsync(int guitarId, int newPairingId)
        {
            await _databaseService.InitializeAsync();

            // Deactivate current active pairing
            await DeactivateCurrentAsync(guitarId);

            // Activate the new pairing
            var newPairing = await GetByIdAsync(newPairingId);
            if (newPairing == null || newPairing.GuitarId != guitarId)
            {
                return false;
            }

            newPairing.IsActive = true;
            newPairing.RemovedAt = null; // Clear removed date since it's now active
            await _databaseService.Connection.UpdateAsync(newPairing);

            return true;
        }

        /// <summary>
        /// Deactivates the currently active pairing for a guitar.
        /// </summary>
        /// <param name="guitarId">The Guitar ID.</param>
        private async Task DeactivateCurrentAsync(int guitarId)
        {
            var currentActive = await GetActiveByGuitarIdAsync(guitarId);
            if (currentActive != null)
            {
                currentActive.IsActive = false;
                currentActive.RemovedAt = DateTime.Now;
                await _databaseService.Connection.UpdateAsync(currentActive);
            }
        }
    }
}

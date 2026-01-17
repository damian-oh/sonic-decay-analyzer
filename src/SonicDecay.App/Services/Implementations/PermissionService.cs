using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Platform-agnostic permission service implementation using MAUI Permissions API.
    /// Provides microphone permission management for audio capture functionality.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        /// <inheritdoc />
        public async Task<PermissionResult> CheckMicrophonePermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            return ConvertToPermissionResult(status);
        }

        /// <inheritdoc />
        public async Task<PermissionResult> RequestMicrophonePermissionAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            return ConvertToPermissionResult(status);
        }

        /// <inheritdoc />
        public async Task<bool> HasMicrophonePermissionAsync()
        {
            var result = await CheckMicrophonePermissionAsync();
            return result == PermissionResult.Granted;
        }

        /// <summary>
        /// Converts MAUI PermissionStatus to application-specific PermissionResult.
        /// </summary>
        /// <param name="status">The MAUI permission status.</param>
        /// <returns>The corresponding PermissionResult value.</returns>
        private static PermissionResult ConvertToPermissionResult(PermissionStatus status)
        {
            return status switch
            {
                PermissionStatus.Granted => PermissionResult.Granted,
                PermissionStatus.Denied => PermissionResult.Denied,
                PermissionStatus.Restricted => PermissionResult.Restricted,
                PermissionStatus.Limited => PermissionResult.Granted, // Limited access is still usable
                PermissionStatus.Unknown => PermissionResult.Unknown,
                _ => PermissionResult.Unknown
            };
        }
    }
}

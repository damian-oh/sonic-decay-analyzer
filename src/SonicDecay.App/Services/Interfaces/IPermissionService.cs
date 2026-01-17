namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Defines the result of a permission request operation.
    /// </summary>
    public enum PermissionResult
    {
        /// <summary>Permission has been granted.</summary>
        Granted,

        /// <summary>Permission has been denied by the user.</summary>
        Denied,

        /// <summary>Permission is restricted by device policy or parental controls.</summary>
        Restricted,

        /// <summary>Permission status is unknown or not determined.</summary>
        Unknown
    }

    /// <summary>
    /// Provides platform-agnostic permission management for application features.
    /// Handles async permission requests and status checking.
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Checks the current status of microphone permission without prompting the user.
        /// </summary>
        /// <returns>The current permission status.</returns>
        Task<PermissionResult> CheckMicrophonePermissionAsync();

        /// <summary>
        /// Requests microphone permission from the user.
        /// May display a system permission dialog if permission has not been determined.
        /// </summary>
        /// <returns>The result of the permission request.</returns>
        Task<PermissionResult> RequestMicrophonePermissionAsync();

        /// <summary>
        /// Gets a value indicating whether microphone permission has been granted.
        /// </summary>
        /// <returns>True if microphone access is permitted; otherwise, false.</returns>
        Task<bool> HasMicrophonePermissionAsync();
    }
}

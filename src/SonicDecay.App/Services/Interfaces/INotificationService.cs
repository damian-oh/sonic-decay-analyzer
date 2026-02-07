namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Provides user-facing notification methods for success and confirmation feedback.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows a brief success notification to the user.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        Task ShowSuccessAsync(string message);

        /// <summary>
        /// Shows an informational alert dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        Task ShowInfoAsync(string title, string message);
    }
}

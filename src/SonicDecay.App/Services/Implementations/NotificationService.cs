using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Provides user-facing notification feedback via DisplayAlert.
    /// </summary>
    public class NotificationService : INotificationService
    {
        /// <inheritdoc />
        public async Task ShowSuccessAsync(string message)
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Success", message, "OK");
            }
        }

        /// <inheritdoc />
        public async Task ShowInfoAsync(string title, string message)
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert(title, message, "OK");
            }
        }
    }
}

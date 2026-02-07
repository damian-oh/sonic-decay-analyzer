using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels providing INotifyPropertyChanged implementation.
    /// Follows MVVM pattern requirements from CLAUDE.md specification.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _title = string.Empty;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel is busy processing.
        /// Use this to show loading indicators in the UI.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the page/view title for navigation display.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets or sets the current error message.
        /// Automatically cleared after a timeout via <see cref="ShowError"/>.
        /// </summary>
        public virtual string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Displays an error message that automatically clears after a timeout.
        /// Uses equality check to avoid clearing a newer error message.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="dismissAfterMs">Milliseconds before auto-dismissal. Default 5000ms.</param>
        protected void ShowError(string message, int dismissAfterMs = 5000)
        {
            ErrorMessage = message;

            _ = AutoClearErrorAsync(message, dismissAfterMs);
        }

        /// <summary>
        /// Clears the current error message.
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Auto-clears the error message after a delay if it hasn't been replaced.
        /// </summary>
        private async Task AutoClearErrorAsync(string originalMessage, int delayMs)
        {
            await Task.Delay(delayMs);

            // Only clear if the message hasn't been replaced by a newer one
            if (ErrorMessage == originalMessage)
            {
                ErrorMessage = string.Empty;
            }
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="backingStore">Reference to the backing field.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (auto-provided by compiler).</param>
        /// <param name="onChanged">Optional action to invoke when value changes.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        protected bool SetProperty<T>(
            ref T backingStore,
            T value,
            [CallerMemberName] string propertyName = "",
            Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return false;
            }

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged for multiple properties.
        /// </summary>
        /// <param name="propertyNames">Names of properties that changed.</param>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanged(name);
            }
        }
    }
}

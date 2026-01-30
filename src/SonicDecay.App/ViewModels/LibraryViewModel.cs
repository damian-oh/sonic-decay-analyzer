using System.Windows.Input;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for the Library hub page.
    /// Provides navigation to Guitars, String Sets, and Pairings management.
    /// </summary>
    public class LibraryViewModel : BaseViewModel
    {
        /// <summary>
        /// Initializes a new instance of the LibraryViewModel class.
        /// </summary>
        public LibraryViewModel()
        {
            Title = "Library";

            NavigateToGuitarsCommand = new AsyncRelayCommand(NavigateToGuitarsAsync);
            NavigateToStringSetsCommand = new AsyncRelayCommand(NavigateToStringSetsAsync);
            NavigateToPairingsCommand = new AsyncRelayCommand(NavigateToPairingsAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        }

        /// <summary>
        /// Command to navigate to the guitars list page.
        /// </summary>
        public ICommand NavigateToGuitarsCommand { get; }

        /// <summary>
        /// Command to navigate to the string sets list page.
        /// </summary>
        public ICommand NavigateToStringSetsCommand { get; }

        /// <summary>
        /// Command to navigate to the pairings management page.
        /// </summary>
        public ICommand NavigateToPairingsCommand { get; }

        /// <summary>
        /// Command to navigate back to the previous page.
        /// </summary>
        public ICommand GoBackCommand { get; }

        private static async Task NavigateToGuitarsAsync()
        {
            await Shell.Current.GoToAsync("GuitarsListPage");
        }

        private static async Task NavigateToStringSetsAsync()
        {
            await Shell.Current.GoToAsync("StringSetsListPage");
        }

        private static async Task NavigateToPairingsAsync()
        {
            await Shell.Current.GoToAsync("PairingsManagementPage");
        }

        private static async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

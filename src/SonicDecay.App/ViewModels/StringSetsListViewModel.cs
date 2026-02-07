using System.Collections.ObjectModel;
using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for the string sets list page.
    /// Displays all string sets and navigates to add/edit string set form.
    /// </summary>
    public class StringSetsListViewModel : BaseViewModel
    {
        private readonly IStringSetRepository _stringSetRepository;

        /// <summary>
        /// Initializes a new instance of the StringSetsListViewModel class.
        /// </summary>
        /// <param name="stringSetRepository">The string set repository.</param>
        public StringSetsListViewModel(IStringSetRepository stringSetRepository)
        {
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));

            Title = "String Sets";
            StringSets = new ObservableCollection<StringSet>();

            LoadCommand = new AsyncRelayCommand(LoadStringSetsAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
            AddStringSetCommand = new AsyncRelayCommand(NavigateToAddStringSetAsync);
            SelectStringSetCommand = new AsyncRelayCommand<StringSet>(NavigateToEditStringSetAsync);
            DeleteStringSetCommand = new AsyncRelayCommand<StringSet>(DeleteStringSetAsync);
        }

        /// <summary>
        /// Gets the collection of string sets.
        /// </summary>
        public ObservableCollection<StringSet> StringSets { get; }

        /// <summary>
        /// Command to load string sets.
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Command to navigate back.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Command to navigate to add string set page.
        /// </summary>
        public ICommand AddStringSetCommand { get; }

        /// <summary>
        /// Command to navigate to edit string set page for the selected item.
        /// </summary>
        public ICommand SelectStringSetCommand { get; }

        /// <summary>
        /// Command to delete a string set with cascade warning confirmation.
        /// </summary>
        public ICommand DeleteStringSetCommand { get; }

        /// <summary>
        /// Gets whether there are no string sets to display.
        /// </summary>
        public bool HasNoStringSets => StringSets.Count == 0;

        /// <summary>
        /// Loads string sets from the repository. Call from OnAppearing.
        /// </summary>
        public async Task LoadStringSetsAsync()
        {
            IsBusy = true;

            try
            {
                var list = await _stringSetRepository.GetAllAsync();
                StringSets.Clear();
                foreach (var s in list)
                {
                    StringSets.Add(s);
                }
                OnPropertyChanged(nameof(HasNoStringSets));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private static async Task NavigateToAddStringSetAsync()
        {
            await Shell.Current.GoToAsync("StringInputPage");
        }

        private static async Task NavigateToEditStringSetAsync(StringSet? stringSet)
        {
            if (stringSet == null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"StringInputPage?id={stringSet.Id}");
        }

        private async Task DeleteStringSetAsync(StringSet? stringSet)
        {
            if (stringSet == null)
            {
                return;
            }

            var confirmed = await Shell.Current.DisplayAlert(
                "Delete String Set",
                $"Are you sure you want to delete \"{stringSet.Brand} {stringSet.Model}\"?\n\nThis will also delete all baselines, measurement logs, and guitar pairings associated with this string set.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                await _stringSetRepository.DeleteAsync(stringSet.Id);
                await LoadStringSetsAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to delete string set: {ex.Message}");
            }
        }
    }
}

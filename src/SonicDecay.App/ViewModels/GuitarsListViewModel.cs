using System.Collections.ObjectModel;
using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for the guitars list page.
    /// Displays all guitars and navigates to add/edit guitar form.
    /// </summary>
    public class GuitarsListViewModel : BaseViewModel
    {
        private readonly IGuitarRepository _guitarRepository;

        /// <summary>
        /// Initializes a new instance of the GuitarsListViewModel class.
        /// </summary>
        /// <param name="guitarRepository">The guitar repository.</param>
        public GuitarsListViewModel(IGuitarRepository guitarRepository)
        {
            _guitarRepository = guitarRepository ?? throw new ArgumentNullException(nameof(guitarRepository));

            Title = "Guitars";
            Guitars = new ObservableCollection<Guitar>();

            LoadCommand = new AsyncRelayCommand(LoadGuitarsAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
            AddGuitarCommand = new AsyncRelayCommand(NavigateToAddGuitarAsync);
            SelectGuitarCommand = new AsyncRelayCommand<Guitar>(NavigateToEditGuitarAsync);
        }

        /// <summary>
        /// Gets the collection of guitars.
        /// </summary>
        public ObservableCollection<Guitar> Guitars { get; }

        /// <summary>
        /// Command to load guitars.
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Command to navigate back.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Command to navigate to add guitar page.
        /// </summary>
        public ICommand AddGuitarCommand { get; }

        /// <summary>
        /// Command to navigate to edit guitar page for the selected guitar.
        /// </summary>
        public ICommand SelectGuitarCommand { get; }

        /// <summary>
        /// Gets whether there are no guitars to display.
        /// </summary>
        public bool HasNoGuitars => Guitars.Count == 0;

        /// <summary>
        /// Loads guitars from the repository. Call from OnAppearing.
        /// </summary>
        public async Task LoadGuitarsAsync()
        {
            IsBusy = true;

            try
            {
                var list = await _guitarRepository.GetAllAsync();
                Guitars.Clear();
                foreach (var g in list)
                {
                    Guitars.Add(g);
                }
                OnPropertyChanged(nameof(HasNoGuitars));
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

        private static async Task NavigateToAddGuitarAsync()
        {
            await Shell.Current.GoToAsync("GuitarInputPage");
        }

        private static async Task NavigateToEditGuitarAsync(Guitar? guitar)
        {
            if (guitar == null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"GuitarInputPage?guitarId={guitar.Id}");
        }
    }
}

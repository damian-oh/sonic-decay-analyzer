using System.Collections.ObjectModel;
using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for managing guitar-string set pairings.
    /// Provides functionality to view, delete, and set active pairings.
    /// </summary>
    public class PairingsManagementViewModel : BaseViewModel
    {
        private readonly IGuitarRepository _guitarRepository;
        private readonly IGuitarStringSetPairingRepository _pairingRepository;
        private readonly IStringSetRepository _stringSetRepository;

        private Guitar? _selectedGuitar;
        private PairingDisplayItem? _selectedPairing;
        private string _errorMessage = string.Empty;
        private bool _hasData;

        /// <summary>
        /// Initializes a new instance of the PairingsManagementViewModel class.
        /// </summary>
        public PairingsManagementViewModel(
            IGuitarRepository guitarRepository,
            IGuitarStringSetPairingRepository pairingRepository,
            IStringSetRepository stringSetRepository)
        {
            _guitarRepository = guitarRepository ?? throw new ArgumentNullException(nameof(guitarRepository));
            _pairingRepository = pairingRepository ?? throw new ArgumentNullException(nameof(pairingRepository));
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));

            Title = "Manage String Sets";

            Guitars = new ObservableCollection<Guitar>();
            Pairings = new ObservableCollection<PairingDisplayItem>();

            // Initialize commands
            DeletePairingCommand = new AsyncRelayCommand<PairingDisplayItem>(DeletePairingAsync, p => p != null);
            SetActiveCommand = new AsyncRelayCommand<PairingDisplayItem>(SetActiveAsync, p => p != null && !p.IsActive);
            RefreshCommand = new AsyncRelayCommand(LoadPairingsAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        }

        #region Properties

        /// <summary>
        /// Gets the collection of available guitars.
        /// </summary>
        public ObservableCollection<Guitar> Guitars { get; }

        /// <summary>
        /// Gets the collection of pairings for the selected guitar.
        /// </summary>
        public ObservableCollection<PairingDisplayItem> Pairings { get; }

        /// <summary>
        /// Gets or sets the currently selected guitar.
        /// </summary>
        public Guitar? SelectedGuitar
        {
            get => _selectedGuitar;
            set
            {
                if (SetProperty(ref _selectedGuitar, value))
                {
                    _ = LoadPairingsAsync();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected pairing.
        /// </summary>
        public PairingDisplayItem? SelectedPairing
        {
            get => _selectedPairing;
            set => SetProperty(ref _selectedPairing, value);
        }

        /// <summary>
        /// Gets or sets the current error message.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets a value indicating whether there is data to display.
        /// </summary>
        public bool HasData
        {
            get => _hasData;
            private set => SetProperty(ref _hasData, value);
        }

        /// <summary>
        /// Gets a value indicating whether there are no pairings.
        /// </summary>
        public bool HasNoPairings => !HasData && SelectedGuitar != null;

        /// <summary>
        /// Gets a value indicating whether there are no guitars.
        /// </summary>
        public bool HasNoGuitars => Guitars.Count == 0;

        #endregion

        #region Commands

        /// <summary>
        /// Command to delete a pairing.
        /// </summary>
        public ICommand DeletePairingCommand { get; }

        /// <summary>
        /// Command to set a pairing as active.
        /// </summary>
        public ICommand SetActiveCommand { get; }

        /// <summary>
        /// Command to refresh the pairings list.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to navigate back.
        /// </summary>
        public ICommand GoBackCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel by loading guitars and pairings.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsBusy = true;

            try
            {
                await LoadGuitarsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadGuitarsAsync()
        {
            var guitars = await _guitarRepository.GetAllAsync();

            Guitars.Clear();
            foreach (var guitar in guitars)
            {
                Guitars.Add(guitar);
            }

            OnPropertyChanged(nameof(HasNoGuitars));

            // Auto-select first guitar if available
            if (Guitars.Count > 0 && SelectedGuitar == null)
            {
                SelectedGuitar = Guitars[0];
            }
        }

        private async Task LoadPairingsAsync()
        {
            if (SelectedGuitar == null)
            {
                Pairings.Clear();
                HasData = false;
                OnPropertyChanged(nameof(HasNoPairings));
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var pairings = await _pairingRepository.GetByGuitarIdAsync(SelectedGuitar.Id);
                var allStringSets = await _stringSetRepository.GetAllAsync();

                Pairings.Clear();
                foreach (var pairing in pairings)
                {
                    var stringSet = allStringSets.FirstOrDefault(s => s.Id == pairing.SetId);
                    if (stringSet != null)
                    {
                        Pairings.Add(new PairingDisplayItem(pairing, stringSet));
                    }
                }

                HasData = Pairings.Count > 0;
                OnPropertyChanged(nameof(HasNoPairings));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load pairings: {ex.Message}";
                HasData = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeletePairingAsync(PairingDisplayItem? pairing)
        {
            if (pairing == null)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                await _pairingRepository.DeleteAsync(pairing.Pairing.Id);
                await LoadPairingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete pairing: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetActiveAsync(PairingDisplayItem? pairing)
        {
            if (pairing == null || SelectedGuitar == null)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                await _pairingRepository.ActivatePairingAsync(SelectedGuitar.Id, pairing.Pairing.Id);
                await LoadPairingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to set active pairing: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        #endregion
    }
}

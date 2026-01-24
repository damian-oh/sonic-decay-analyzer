using System.Collections.ObjectModel;
using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for creating and editing guitars.
    /// Manages guitar CRUD operations and string set pairing.
    /// </summary>
    public class GuitarInputViewModel : BaseViewModel
    {
        private readonly IGuitarRepository _guitarRepository;
        private readonly IGuitarStringSetPairingRepository _pairingRepository;
        private readonly IStringSetRepository _stringSetRepository;

        private string _name = string.Empty;
        private string? _make;
        private string? _model;
        private string _type = "Electric";
        private string? _notes;
        private string _errorMessage = string.Empty;
        private bool _isEditing;
        private int? _editingGuitarId;
        private StringSet? _selectedStringSet;

        /// <summary>
        /// Available guitar types.
        /// </summary>
        public static readonly string[] GuitarTypes = { "Electric", "Acoustic", "Classical", "Bass" };

        /// <summary>
        /// Initializes a new instance of the GuitarInputViewModel class.
        /// </summary>
        public GuitarInputViewModel(
            IGuitarRepository guitarRepository,
            IGuitarStringSetPairingRepository pairingRepository,
            IStringSetRepository stringSetRepository)
        {
            _guitarRepository = guitarRepository ?? throw new ArgumentNullException(nameof(guitarRepository));
            _pairingRepository = pairingRepository ?? throw new ArgumentNullException(nameof(pairingRepository));
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));

            Title = "Add Guitar";

            // Initialize collections
            AvailableTypes = new ObservableCollection<string>(GuitarTypes);
            AvailableStringSets = new ObservableCollection<StringSet>();

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
            SelectTypeCommand = new RelayCommand<string>(SelectType);
        }

        #region Properties

        /// <summary>
        /// Gets the collection of available guitar types.
        /// </summary>
        public ObservableCollection<string> AvailableTypes { get; }

        /// <summary>
        /// Gets the collection of available string sets for pairing.
        /// </summary>
        public ObservableCollection<StringSet> AvailableStringSets { get; }

        /// <summary>
        /// Gets or sets the user-defined guitar name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    UpdateSaveCommand();
                }
            }
        }

        /// <summary>
        /// Gets or sets the guitar manufacturer/brand.
        /// </summary>
        public string? Make
        {
            get => _make;
            set => SetProperty(ref _make, value);
        }

        /// <summary>
        /// Gets or sets the guitar model name.
        /// </summary>
        public string? Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// Gets or sets the guitar type (Electric, Acoustic, Classical, Bass).
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(IsElectric));
                    OnPropertyChanged(nameof(IsAcoustic));
                    OnPropertyChanged(nameof(IsClassical));
                    OnPropertyChanged(nameof(IsBass));
                }
            }
        }

        /// <summary>
        /// Gets or sets optional notes about the guitar.
        /// </summary>
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
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
        /// Gets a value indicating whether this is editing an existing guitar.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            private set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    Title = value ? "Edit Guitar" : "Add Guitar";
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected string set for initial pairing.
        /// Only used when creating a new guitar.
        /// </summary>
        public StringSet? SelectedStringSet
        {
            get => _selectedStringSet;
            set => SetProperty(ref _selectedStringSet, value);
        }

        // Type selection helpers for UI binding
        public bool IsElectric => Type == "Electric";
        public bool IsAcoustic => Type == "Acoustic";
        public bool IsClassical => Type == "Classical";
        public bool IsBass => Type == "Bass";

        #endregion

        #region Commands

        /// <summary>
        /// Command to save the guitar.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command to cancel and navigate back.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Command to select a guitar type.
        /// </summary>
        public ICommand SelectTypeCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel by loading available string sets.
        /// Call this from the page's OnAppearing event.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsBusy = true;

            try
            {
                var stringSets = await _stringSetRepository.GetAllAsync();
                AvailableStringSets.Clear();
                foreach (var set in stringSets)
                {
                    AvailableStringSets.Add(set);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load string sets: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Loads an existing guitar for editing.
        /// </summary>
        /// <param name="guitarId">The ID of the guitar to edit.</param>
        public async Task LoadForEditAsync(int guitarId)
        {
            IsBusy = true;

            try
            {
                var guitar = await _guitarRepository.GetByIdAsync(guitarId);
                if (guitar != null)
                {
                    _editingGuitarId = guitarId;
                    IsEditing = true;

                    Name = guitar.Name;
                    Make = guitar.Make;
                    Model = guitar.Model;
                    Type = guitar.Type;
                    Notes = guitar.Notes;

                    // Load current active string set
                    var activePairing = await _pairingRepository.GetActiveByGuitarIdAsync(guitarId);
                    if (activePairing != null)
                    {
                        SelectedStringSet = AvailableStringSets.FirstOrDefault(s => s.Id == activePairing.SetId);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load guitar: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Resets the form for creating a new guitar.
        /// </summary>
        public void Reset()
        {
            _editingGuitarId = null;
            IsEditing = false;

            Name = string.Empty;
            Make = null;
            Model = null;
            Type = "Electric";
            Notes = null;
            SelectedStringSet = null;
            ErrorMessage = string.Empty;
        }

        #endregion

        #region Private Methods

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        private async Task SaveAsync()
        {
            if (!CanSave())
            {
                ErrorMessage = "Guitar name is required";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                if (IsEditing && _editingGuitarId.HasValue)
                {
                    await UpdateExistingGuitarAsync();
                }
                else
                {
                    await CreateNewGuitarAsync();
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateNewGuitarAsync()
        {
            var guitar = new Guitar
            {
                Name = Name.Trim(),
                Make = Make?.Trim(),
                Model = Model?.Trim(),
                Type = Type,
                Notes = Notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            await _guitarRepository.CreateAsync(guitar);

            // Create initial string set pairing if selected
            if (SelectedStringSet != null)
            {
                var pairing = new GuitarStringSetPairing
                {
                    GuitarId = guitar.Id,
                    SetId = SelectedStringSet.Id,
                    InstalledAt = DateTime.Now,
                    IsActive = true
                };

                await _pairingRepository.CreateAsync(pairing);
            }
        }

        private async Task UpdateExistingGuitarAsync()
        {
            if (!_editingGuitarId.HasValue)
            {
                return;
            }

            var existingGuitar = await _guitarRepository.GetByIdAsync(_editingGuitarId.Value);
            if (existingGuitar == null)
            {
                ErrorMessage = "Guitar not found";
                return;
            }

            existingGuitar.Name = Name.Trim();
            existingGuitar.Make = Make?.Trim();
            existingGuitar.Model = Model?.Trim();
            existingGuitar.Type = Type;
            existingGuitar.Notes = Notes?.Trim();

            await _guitarRepository.UpdateAsync(existingGuitar);

            // Update string set pairing if changed
            if (SelectedStringSet != null)
            {
                var currentPairing = await _pairingRepository.GetActiveByGuitarIdAsync(_editingGuitarId.Value);
                if (currentPairing == null || currentPairing.SetId != SelectedStringSet.Id)
                {
                    // Create new pairing (which will deactivate the current one)
                    var newPairing = new GuitarStringSetPairing
                    {
                        GuitarId = _editingGuitarId.Value,
                        SetId = SelectedStringSet.Id,
                        InstalledAt = DateTime.Now,
                        IsActive = true
                    };

                    await _pairingRepository.CreateAsync(newPairing);
                }
            }
        }

        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void SelectType(string? type)
        {
            if (!string.IsNullOrEmpty(type) && GuitarTypes.Contains(type))
            {
                Type = type;
            }
        }

        private void UpdateSaveCommand()
        {
            (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}

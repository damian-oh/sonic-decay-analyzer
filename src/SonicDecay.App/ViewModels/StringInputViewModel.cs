using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for creating and editing custom string sets.
    /// Use this for string sets not included in the preset seed data.
    /// </summary>
    public class StringInputViewModel : BaseViewModel
    {
        private readonly IStringSetRepository _stringSetRepository;
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly INotificationService _notificationService;

        private string _brand = string.Empty;
        private string _model = string.Empty;

        // Individual string gauges (inches)
        private double _gaugeE1 = 0.010;
        private double _gaugeB2 = 0.013;
        private double _gaugeG3 = 0.017;
        private double _gaugeD4 = 0.026;
        private double _gaugeA5 = 0.036;
        private double _gaugeE6 = 0.046;

        private bool _isEditing;
        private int? _editingSetId;
        private string? _selectedPreset = "light"; // Default to light gauge
        private string _brandError = string.Empty;
        private string _modelError = string.Empty;
        private string _gaugeError = string.Empty;

        /// <summary>
        /// Standard tuning fundamental frequencies for each string.
        /// </summary>
        private static readonly double[] StandardTuningFrequencies =
        {
            329.63, // E4 - High E (string 1)
            246.94, // B3 (string 2)
            196.00, // G3 (string 3)
            146.83, // D3 (string 4)
            110.00, // A2 (string 5)
            82.41   // E2 - Low E (string 6)
        };

        /// <summary>
        /// Initializes a new instance of the StringInputViewModel class.
        /// </summary>
        public StringInputViewModel(
            IStringSetRepository stringSetRepository,
            IStringBaselineRepository baselineRepository,
            INotificationService notificationService)
        {
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            Title = "Custom String Set";

            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
            ApplyPresetCommand = new RelayCommand<string>(ApplyGaugePreset);
        }

        #region Properties

        /// <summary>
        /// Gets or sets the string brand (e.g., "Elixir", "D'Addario").
        /// </summary>
        public string Brand
        {
            get => _brand;
            set
            {
                if (SetProperty(ref _brand, value))
                {
                    BrandError = string.IsNullOrWhiteSpace(value) ? "Brand is required" : string.Empty;
                    UpdateSaveCommand();
                }
            }
        }

        /// <summary>
        /// Gets the inline validation error for the Brand field.
        /// </summary>
        public string BrandError
        {
            get => _brandError;
            private set => SetProperty(ref _brandError, value);
        }

        /// <summary>
        /// Gets or sets the string model (e.g., "Nanoweb", "EXL110").
        /// </summary>
        public string Model
        {
            get => _model;
            set
            {
                if (SetProperty(ref _model, value))
                {
                    ModelError = string.IsNullOrWhiteSpace(value) ? "Model is required" : string.Empty;
                    UpdateSaveCommand();
                }
            }
        }

        /// <summary>
        /// Gets the inline validation error for the Model field.
        /// </summary>
        public string ModelError
        {
            get => _modelError;
            private set => SetProperty(ref _modelError, value);
        }

        /// <summary>
        /// Gets or sets the high E string gauge (string 1).
        /// </summary>
        public double GaugeE1
        {
            get => _gaugeE1;
            set { if (SetProperty(ref _gaugeE1, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets or sets the B string gauge (string 2).
        /// </summary>
        public double GaugeB2
        {
            get => _gaugeB2;
            set { if (SetProperty(ref _gaugeB2, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets or sets the G string gauge (string 3).
        /// </summary>
        public double GaugeG3
        {
            get => _gaugeG3;
            set { if (SetProperty(ref _gaugeG3, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets or sets the D string gauge (string 4).
        /// </summary>
        public double GaugeD4
        {
            get => _gaugeD4;
            set { if (SetProperty(ref _gaugeD4, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets or sets the A string gauge (string 5).
        /// </summary>
        public double GaugeA5
        {
            get => _gaugeA5;
            set { if (SetProperty(ref _gaugeA5, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets or sets the low E string gauge (string 6).
        /// </summary>
        public double GaugeE6
        {
            get => _gaugeE6;
            set { if (SetProperty(ref _gaugeE6, value)) { ValidateGauges(); UpdateSaveCommand(); } }
        }

        /// <summary>
        /// Gets the gauge validation error text, if any.
        /// </summary>
        public string GaugeError
        {
            get => _gaugeError;
            private set => SetProperty(ref _gaugeError, value);
        }

        /// <summary>
        /// Gets a value indicating whether this is editing an existing set.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            private set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    Title = value ? "Edit String Set" : "Custom String Set";
                }
            }
        }

        /// <summary>
        /// Gets the gauge summary for display.
        /// </summary>
        public string GaugeSummary => $"{GaugeE1:F3}-{GaugeE6:F3}";

        /// <summary>
        /// Gets or sets the currently selected gauge preset.
        /// Used to highlight the active preset button.
        /// </summary>
        public string? SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to save the string set.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Command to cancel and navigate back.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Command to apply a gauge preset configuration.
        /// </summary>
        public ICommand ApplyPresetCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads an existing string set for editing.
        /// </summary>
        /// <param name="stringSetId">The ID of the string set to edit.</param>
        public async Task LoadForEditAsync(int stringSetId)
        {
            IsBusy = true;

            try
            {
                var stringSet = await _stringSetRepository.GetByIdAsync(stringSetId);
                if (stringSet != null)
                {
                    _editingSetId = stringSetId;
                    IsEditing = true;

                    Brand = stringSet.Brand;
                    Model = stringSet.Model;
                    GaugeE1 = stringSet.GaugeE1;
                    GaugeB2 = stringSet.GaugeB2;
                    GaugeG3 = stringSet.GaugeG3;
                    GaugeD4 = stringSet.GaugeD4;
                    GaugeA5 = stringSet.GaugeA5;
                    GaugeE6 = stringSet.GaugeE6;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load string set: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Resets the form for creating a new string set.
        /// </summary>
        public void Reset()
        {
            _editingSetId = null;
            IsEditing = false;

            Brand = string.Empty;
            Model = string.Empty;

            // Reset to common light gauge defaults
            GaugeE1 = 0.010;
            GaugeB2 = 0.013;
            GaugeG3 = 0.017;
            GaugeD4 = 0.026;
            GaugeA5 = 0.036;
            GaugeE6 = 0.046;

            // Reset preset selection to match light gauge
            SelectedPreset = "light";

            ClearError();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Minimum valid string gauge in inches (0.006" for ultra-light high E).
        /// </summary>
        private const double MinGauge = 0.006;

        /// <summary>
        /// Maximum valid string gauge in inches (0.080" for heaviest bass strings).
        /// </summary>
        private const double MaxGauge = 0.080;

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Brand) &&
                   !string.IsNullOrWhiteSpace(Model) &&
                   AreGaugesValid();
        }

        /// <summary>
        /// Validates that all 6 gauge values fall within a physically reasonable range.
        /// </summary>
        private bool AreGaugesValid()
        {
            return IsGaugeInRange(GaugeE1) && IsGaugeInRange(GaugeB2) &&
                   IsGaugeInRange(GaugeG3) && IsGaugeInRange(GaugeD4) &&
                   IsGaugeInRange(GaugeA5) && IsGaugeInRange(GaugeE6);
        }

        private static bool IsGaugeInRange(double gauge)
        {
            return gauge >= MinGauge && gauge <= MaxGauge;
        }

        private async Task SaveAsync()
        {
            if (!CanSave())
            {
                if (string.IsNullOrWhiteSpace(Brand) || string.IsNullOrWhiteSpace(Model))
                {
                    ShowError("Brand and Model are required");
                }
                else if (!AreGaugesValid())
                {
                    ShowError($"All gauge values must be between {MinGauge:F3}\" and {MaxGauge:F3}\"");
                }
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                if (IsEditing && _editingSetId.HasValue)
                {
                    await UpdateExistingSetAsync();
                    await _notificationService.ShowSuccessAsync("String set updated successfully.");
                }
                else
                {
                    await CreateNewSetAsync();
                    await _notificationService.ShowSuccessAsync("String set saved successfully.");
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateNewSetAsync()
        {
            var stringSet = new StringSet
            {
                Brand = Brand.Trim(),
                Model = Model.Trim(),
                GaugeE1 = GaugeE1,
                GaugeB2 = GaugeB2,
                GaugeG3 = GaugeG3,
                GaugeD4 = GaugeD4,
                GaugeA5 = GaugeA5,
                GaugeE6 = GaugeE6,
                CreatedAt = DateTime.Now
            };

            await _stringSetRepository.CreateAsync(stringSet);

            // Create 6 baselines for the new string set
            var baselines = new List<StringBaseline>();
            for (int i = 1; i <= 6; i++)
            {
                baselines.Add(new StringBaseline
                {
                    SetId = stringSet.Id,
                    StringNumber = i,
                    FundamentalFreq = StandardTuningFrequencies[i - 1],
                    InitialCentroid = 0, // Will be set when baseline is established
                    InitialHighRatio = 0,
                    CreatedAt = DateTime.Now
                });
            }

            await _baselineRepository.CreateBatchAsync(baselines);
        }

        private async Task UpdateExistingSetAsync()
        {
            if (!_editingSetId.HasValue)
            {
                return;
            }

            var existingSet = await _stringSetRepository.GetByIdAsync(_editingSetId.Value);
            if (existingSet == null)
            {
                ShowError("String set not found");
                return;
            }

            existingSet.Brand = Brand.Trim();
            existingSet.Model = Model.Trim();
            existingSet.GaugeE1 = GaugeE1;
            existingSet.GaugeB2 = GaugeB2;
            existingSet.GaugeG3 = GaugeG3;
            existingSet.GaugeD4 = GaugeD4;
            existingSet.GaugeA5 = GaugeA5;
            existingSet.GaugeE6 = GaugeE6;

            await _stringSetRepository.UpdateAsync(existingSet);
        }

        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void ApplyGaugePreset(string? preset)
        {
            var normalizedPreset = preset?.ToLowerInvariant();

            switch (normalizedPreset)
            {
                case "extralight":
                    // Extra Light: .008-.038
                    GaugeE1 = 0.008; GaugeB2 = 0.010; GaugeG3 = 0.015;
                    GaugeD4 = 0.021; GaugeA5 = 0.030; GaugeE6 = 0.038;
                    break;

                case "light":
                    // Light: .010-.046
                    GaugeE1 = 0.010; GaugeB2 = 0.013; GaugeG3 = 0.017;
                    GaugeD4 = 0.026; GaugeA5 = 0.036; GaugeE6 = 0.046;
                    break;

                case "medium":
                    // Medium: .011-.050
                    GaugeE1 = 0.011; GaugeB2 = 0.014; GaugeG3 = 0.018;
                    GaugeD4 = 0.028; GaugeA5 = 0.038; GaugeE6 = 0.050;
                    break;

                case "heavy":
                    // Heavy: .012-.054
                    GaugeE1 = 0.012; GaugeB2 = 0.016; GaugeG3 = 0.020;
                    GaugeD4 = 0.032; GaugeA5 = 0.042; GaugeE6 = 0.054;
                    break;

                case "jazz":
                    // Jazz: .013-.056
                    GaugeE1 = 0.013; GaugeB2 = 0.017; GaugeG3 = 0.026;
                    GaugeD4 = 0.036; GaugeA5 = 0.046; GaugeE6 = 0.056;
                    break;

                default:
                    return; // Don't update SelectedPreset for unknown presets
            }

            // Update selected preset for button highlighting
            SelectedPreset = normalizedPreset;

            // Notify all gauge properties changed
            OnPropertiesChanged(
                nameof(GaugeE1), nameof(GaugeB2), nameof(GaugeG3),
                nameof(GaugeD4), nameof(GaugeA5), nameof(GaugeE6),
                nameof(GaugeSummary));
        }

        private void ValidateGauges()
        {
            GaugeError = AreGaugesValid()
                ? string.Empty
                : $"Gauges must be {MinGauge:F3}\"-{MaxGauge:F3}\"";
        }

        private void UpdateSaveCommand()
        {
            (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}

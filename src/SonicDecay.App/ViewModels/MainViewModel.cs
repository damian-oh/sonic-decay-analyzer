using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// Main view model for the primary analysis interface.
    /// Orchestrates audio capture, spectral analysis, decay visualization,
    /// and predictive maintenance recommendations.
    /// </summary>
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly IAudioCaptureService _audioCaptureService;
        private readonly IMeasurementService _measurementService;
        private readonly IStringSetRepository _stringSetRepository;
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly IPermissionService _permissionService;
        private readonly IRecommendationService _recommendationService;
        private readonly IGuitarRepository _guitarRepository;
        private readonly IGuitarStringSetPairingRepository _pairingRepository;

        private bool _isCapturing;
        private bool _isAnalyzing;
        private bool _hasPermission;
        private string _statusMessage = "Ready to analyze";
        private string _errorMessage = string.Empty;

        // Guitar selection
        private Guitar? _selectedGuitar;

        // Guitar-String pairing selection
        private PairingDisplayItem? _selectedPairingItem;

        // Brand/Model selection for presets
        private List<StringSet> _allStringSets = new();
        private string? _selectedBrand;

        // Current string set selection
        private StringSet? _selectedStringSet;
        private StringBaseline? _selectedBaseline;
        private int _selectedStringNumber = 1;

        // Real-time analysis results
        private double _currentCentroid;
        private double _currentHfRatio;
        private double _decayPercentage;
        private double _fundamentalFreq;
        private double _rmsLevel;

        // Health indicator
        private string _healthStatus = "Unknown";
        private Color _healthColor = Colors.Gray;

        // Recommendation state
        private ReplacementRecommendation? _recommendation;
        private bool _isLoadingRecommendation;

        // Context section expand/collapse
        private bool _isContextExpanded = true;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(
            IAudioCaptureService audioCaptureService,
            IMeasurementService measurementService,
            IStringSetRepository stringSetRepository,
            IStringBaselineRepository baselineRepository,
            IPermissionService permissionService,
            IRecommendationService recommendationService,
            IGuitarRepository guitarRepository,
            IGuitarStringSetPairingRepository pairingRepository)
        {
            _audioCaptureService = audioCaptureService ?? throw new ArgumentNullException(nameof(audioCaptureService));
            _measurementService = measurementService ?? throw new ArgumentNullException(nameof(measurementService));
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _guitarRepository = guitarRepository ?? throw new ArgumentNullException(nameof(guitarRepository));
            _pairingRepository = pairingRepository ?? throw new ArgumentNullException(nameof(pairingRepository));

            Title = "Sonic Decay Analyzer";

            // Initialize collections
            Guitars = new ObservableCollection<Guitar>();
            AvailablePairings = new ObservableCollection<PairingDisplayItem>();
            StringSets = new ObservableCollection<StringSet>();
            AvailableBrands = new ObservableCollection<string>();
            FilteredModels = new ObservableCollection<StringSet>();
            DecayHistory = new ObservableCollection<MeasurementLog>();
            StringNumbers = new ObservableCollection<StringNumberItem>
            {
                new(1, "High E (1)"),
                new(2, "B (2)"),
                new(3, "G (3)"),
                new(4, "D (4)"),
                new(5, "A (5)"),
                new(6, "Low E (6)")
            };

            // Initialize commands
            StartCaptureCommand = new AsyncRelayCommand(StartCaptureAsync, () => !IsCapturing && HasPermission && CanStartCapture());
            StopCaptureCommand = new AsyncRelayCommand(StopCaptureAsync, () => IsCapturing);
            RequestPermissionCommand = new AsyncRelayCommand(RequestPermissionAsync, () => !HasPermission);
            RefreshStringSetsCommand = new AsyncRelayCommand(LoadStringSetsAsync);
            EstablishBaselineCommand = new AsyncRelayCommand(EstablishBaselineAsync, () => IsCapturing && SelectedBaseline != null);
            NavigateToStringInputCommand = new AsyncRelayCommand(NavigateToStringInputAsync);
            NavigateToGuitarInputCommand = new AsyncRelayCommand(NavigateToGuitarInputAsync);
            NavigateToChartCommand = new AsyncRelayCommand(NavigateToChartAsync, () => SelectedBaseline != null && DecayHistory.Count > 0);
            RefreshRecommendationCommand = new AsyncRelayCommand(RefreshRecommendationAsync, () => SelectedBaseline != null && !IsLoadingRecommendation);
            DeletePairingCommand = new AsyncRelayCommand(DeleteSelectedPairingAsync, () => _selectedPairingItem != null && !IsCapturing);
            NavigateToPairingsCommand = new AsyncRelayCommand(NavigateToPairingsAsync);
            NavigateToLibraryCommand = new AsyncRelayCommand(NavigateToLibraryAsync);
            ToggleContextExpandedCommand = new RelayCommand(ToggleContextExpanded);

            // Subscribe to audio capture events
            _audioCaptureService.BufferCaptured += OnBufferCaptured;
            _audioCaptureService.StateChanged += OnCaptureStateChanged;
            _audioCaptureService.ErrorOccurred += OnCaptureError;
        }

        #region Properties

        /// <summary>
        /// Gets the collection of available guitars.
        /// </summary>
        public ObservableCollection<Guitar> Guitars { get; }

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
                    _ = LoadPairingsForGuitarAsync();
                    UpdateCommandStates();
                    OnPropertyChanged(nameof(ContextSummaryText));
                }
            }
        }

        /// <summary>
        /// Gets the collection of available pairings for the selected guitar.
        /// </summary>
        public ObservableCollection<PairingDisplayItem> AvailablePairings { get; }

        /// <summary>
        /// Gets or sets the currently selected guitar-string pairing.
        /// </summary>
        public PairingDisplayItem? SelectedPairing
        {
            get => _selectedPairingItem;
            set
            {
                if (SetProperty(ref _selectedPairingItem, value))
                {
                    _ = LoadStringSetFromPairingAsync();
                    OnPropertyChanged(nameof(HasPairings));
                    UpdateCommandStates();
                    OnPropertyChanged(nameof(ContextSummaryText));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are pairings available for the selected guitar.
        /// </summary>
        public bool HasPairings => AvailablePairings.Count > 0;

        /// <summary>
        /// Gets the collection of available string sets.
        /// </summary>
        public ObservableCollection<StringSet> StringSets { get; }

        /// <summary>
        /// Gets the collection of available brands from preset string sets.
        /// </summary>
        public ObservableCollection<string> AvailableBrands { get; }

        /// <summary>
        /// Gets the collection of string set models filtered by selected brand.
        /// </summary>
        public ObservableCollection<StringSet> FilteredModels { get; }

        /// <summary>
        /// Gets the collection of measurement history for decay trend visualization.
        /// </summary>
        public ObservableCollection<MeasurementLog> DecayHistory { get; }

        /// <summary>
        /// Gets the collection of string number options (1-6).
        /// </summary>
        public ObservableCollection<StringNumberItem> StringNumbers { get; }

        /// <summary>
        /// Gets or sets the selected brand for filtering available models.
        /// </summary>
        public string? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (SetProperty(ref _selectedBrand, value))
                {
                    FilterModelsByBrand();
                    // Clear selected model when brand changes
                    SelectedStringSet = null;
                    UpdateCommandStates();
                    OnPropertyChanged(nameof(ContextSummaryText));
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected string set (model).
        /// </summary>
        public StringSet? SelectedStringSet
        {
            get => _selectedStringSet;
            set
            {
                if (SetProperty(ref _selectedStringSet, value))
                {
                    _ = LoadBaselineForStringAsync();
                    _ = UpdateAllBaselineStatusAsync();
                    UpdateCommandStates();
                    OnPropertyChanged(nameof(ContextSummaryText));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected string number (1-6).
        /// </summary>
        public int SelectedStringNumber
        {
            get => _selectedStringNumber;
            set
            {
                if (SetProperty(ref _selectedStringNumber, value))
                {
                    _ = LoadBaselineForStringAsync();
                    OnPropertyChanged(nameof(ContextSummaryText));
                }
            }
        }

        /// <summary>
        /// Gets the baseline for the currently selected string.
        /// </summary>
        public StringBaseline? SelectedBaseline
        {
            get => _selectedBaseline;
            private set
            {
                if (SetProperty(ref _selectedBaseline, value))
                {
                    OnPropertyChanged(nameof(HasBaseline));
                    OnPropertyChanged(nameof(BaselineStatus));
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether audio capture is currently active.
        /// </summary>
        public bool IsCapturing
        {
            get => _isCapturing;
            private set
            {
                if (SetProperty(ref _isCapturing, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether spectral analysis is in progress.
        /// </summary>
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            private set => SetProperty(ref _isAnalyzing, value);
        }

        /// <summary>
        /// Gets a value indicating whether microphone permission has been granted.
        /// </summary>
        public bool HasPermission
        {
            get => _hasPermission;
            private set
            {
                if (SetProperty(ref _hasPermission, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the selected baseline has been established.
        /// </summary>
        public bool HasBaseline => SelectedBaseline?.InitialCentroid > 0;

        /// <summary>
        /// Gets a description of the current baseline status.
        /// </summary>
        public string BaselineStatus => HasBaseline
            ? $"Baseline: {SelectedBaseline!.InitialCentroid:F1} Hz centroid"
            : "No baseline - establish fresh string reference";

        /// <summary>
        /// Gets or sets whether the context section is expanded.
        /// </summary>
        public bool IsContextExpanded
        {
            get => _isContextExpanded;
            set => SetProperty(ref _isContextExpanded, value);
        }

        /// <summary>
        /// Gets a one-line summary of the current selection (guitar, string set, string number).
        /// </summary>
        public string ContextSummaryText
        {
            get
            {
                var guitar = SelectedGuitar?.Name ?? "—";
                var stringSet = SelectedStringSet != null ? $"{SelectedStringSet.Brand} {SelectedStringSet.Model}" : "—";
                var stringLabel = SelectedStringNumber >= 1 && SelectedStringNumber <= 6 && StringNumbers.Count >= SelectedStringNumber
                    ? StringNumbers[SelectedStringNumber - 1].DisplayName
                    : "—";
                return $"{guitar} • {stringSet} • {stringLabel}";
            }
        }

        /// <summary>
        /// Gets or sets the current status message.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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
        /// Gets the current spectral centroid (brightness indicator) in Hz.
        /// </summary>
        public double CurrentCentroid
        {
            get => _currentCentroid;
            private set => SetProperty(ref _currentCentroid, value);
        }

        /// <summary>
        /// Gets the current high-frequency energy ratio.
        /// </summary>
        public double CurrentHfRatio
        {
            get => _currentHfRatio;
            private set => SetProperty(ref _currentHfRatio, value);
        }

        /// <summary>
        /// Gets the calculated decay percentage relative to baseline.
        /// Positive values indicate string degradation.
        /// </summary>
        public double DecayPercentage
        {
            get => _decayPercentage;
            private set
            {
                if (SetProperty(ref _decayPercentage, value))
                {
                    UpdateHealthIndicator();
                }
            }
        }

        /// <summary>
        /// Gets the detected fundamental frequency in Hz.
        /// </summary>
        public double FundamentalFreq
        {
            get => _fundamentalFreq;
            private set => SetProperty(ref _fundamentalFreq, value);
        }

        /// <summary>
        /// Gets the current RMS audio level (0-1).
        /// </summary>
        public double RmsLevel
        {
            get => _rmsLevel;
            private set => SetProperty(ref _rmsLevel, value);
        }

        /// <summary>
        /// Gets the current health status description.
        /// </summary>
        public string HealthStatus
        {
            get => _healthStatus;
            private set => SetProperty(ref _healthStatus, value);
        }

        /// <summary>
        /// Gets the color-coded health indicator.
        /// </summary>
        public Color HealthColor
        {
            get => _healthColor;
            private set => SetProperty(ref _healthColor, value);
        }

        /// <summary>
        /// Gets the current replacement recommendation based on decay trend analysis.
        /// </summary>
        public ReplacementRecommendation? Recommendation
        {
            get => _recommendation;
            private set
            {
                if (SetProperty(ref _recommendation, value))
                {
                    OnPropertyChanged(nameof(HasRecommendation));
                    OnPropertyChanged(nameof(RecommendationUrgencyText));
                    OnPropertyChanged(nameof(RecommendationUrgencyColor));
                    OnPropertyChanged(nameof(EstimatedHoursRemainingText));
                    OnPropertyChanged(nameof(ProjectedReplacementDateText));
                    OnPropertyChanged(nameof(RecommendationConfidenceText));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether recommendation data is available.
        /// </summary>
        public bool HasRecommendation => Recommendation != null;

        /// <summary>
        /// Gets a value indicating whether recommendation is being loaded.
        /// </summary>
        public bool IsLoadingRecommendation
        {
            get => _isLoadingRecommendation;
            private set
            {
                if (SetProperty(ref _isLoadingRecommendation, value))
                {
                    (RefreshRecommendationCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets the urgency level as display text.
        /// </summary>
        public string RecommendationUrgencyText => Recommendation?.Urgency switch
        {
            ReplacementUrgency.None => "Excellent",
            ReplacementUrgency.Monitor => "Monitor",
            ReplacementUrgency.Soon => "Soon",
            ReplacementUrgency.Recommended => "Recommended",
            ReplacementUrgency.Urgent => "Urgent",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets the color for the urgency level indicator.
        /// </summary>
        public Color RecommendationUrgencyColor => Recommendation?.Urgency switch
        {
            ReplacementUrgency.None => Color.FromArgb("#22c55e"),       // Green
            ReplacementUrgency.Monitor => Color.FromArgb("#3b82f6"),    // Blue
            ReplacementUrgency.Soon => Color.FromArgb("#eab308"),       // Yellow
            ReplacementUrgency.Recommended => Color.FromArgb("#f97316"), // Orange
            ReplacementUrgency.Urgent => Color.FromArgb("#ef4444"),     // Red
            _ => Colors.Gray
        };

        /// <summary>
        /// Gets the estimated remaining hours as display text.
        /// </summary>
        public string EstimatedHoursRemainingText => Recommendation?.EstimatedHoursRemaining.HasValue == true
            ? $"{Recommendation.EstimatedHoursRemaining.Value:F0} hours"
            : "—";

        /// <summary>
        /// Gets the projected replacement date as display text.
        /// </summary>
        public string ProjectedReplacementDateText => Recommendation?.ProjectedReplacementDate.HasValue == true
            ? Recommendation.ProjectedReplacementDate.Value.ToString("MMM d, yyyy")
            : "—";

        /// <summary>
        /// Gets the recommendation confidence as display text.
        /// </summary>
        public string RecommendationConfidenceText => Recommendation != null
            ? $"{Recommendation.ConfidencePercentage:F0}%"
            : "—";

        #endregion

        #region Commands

        /// <summary>
        /// Command to start audio capture.
        /// </summary>
        public ICommand StartCaptureCommand { get; }

        /// <summary>
        /// Command to stop audio capture.
        /// </summary>
        public ICommand StopCaptureCommand { get; }

        /// <summary>
        /// Command to request microphone permission.
        /// </summary>
        public ICommand RequestPermissionCommand { get; }

        /// <summary>
        /// Command to refresh the string sets list.
        /// </summary>
        public ICommand RefreshStringSetsCommand { get; }

        /// <summary>
        /// Command to establish baseline for current string.
        /// </summary>
        public ICommand EstablishBaselineCommand { get; }

        /// <summary>
        /// Command to navigate to string input page.
        /// </summary>
        public ICommand NavigateToStringInputCommand { get; }

        /// <summary>
        /// Command to navigate to guitar input page.
        /// </summary>
        public ICommand NavigateToGuitarInputCommand { get; }

        /// <summary>
        /// Command to navigate to full decay chart page.
        /// </summary>
        public ICommand NavigateToChartCommand { get; }

        /// <summary>
        /// Command to refresh the replacement recommendation.
        /// </summary>
        public ICommand RefreshRecommendationCommand { get; }

        /// <summary>
        /// Command to delete the selected guitar-string pairing.
        /// </summary>
        public ICommand DeletePairingCommand { get; }

        /// <summary>
        /// Command to navigate to the pairings management page.
        /// </summary>
        public ICommand NavigateToPairingsCommand { get; }

        /// <summary>
        /// Command to navigate to the library hub page.
        /// </summary>
        public ICommand NavigateToLibraryCommand { get; }

        /// <summary>
        /// Command to toggle the context section expanded/collapsed state.
        /// </summary>
        public ICommand ToggleContextExpandedCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel by loading data and checking permissions.
        /// Call this from the page's OnAppearing event.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsBusy = true;

            try
            {
                // Check microphone permission
                HasPermission = await _permissionService.HasMicrophonePermissionAsync();

                // Load guitars and string sets
                await LoadGuitarsAsync();
                await LoadStringSetsAsync();

                StatusMessage = HasPermission
                    ? "Ready - select a guitar and string set to begin"
                    : "Microphone permission required";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Initialization failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task StartCaptureAsync()
        {
            // Validate all required selections
            if (string.IsNullOrEmpty(SelectedBrand))
            {
                ErrorMessage = "Please select a string brand first";
                return;
            }

            if (SelectedStringSet == null)
            {
                ErrorMessage = "Please select a string model first";
                return;
            }

            if (SelectedStringNumber < 1 || SelectedStringNumber > 6)
            {
                ErrorMessage = "Please select a string number (1-6)";
                return;
            }

            if (SelectedBaseline == null)
            {
                ErrorMessage = "No baseline found for this string - please ensure string set is properly configured";
                return;
            }

            ErrorMessage = string.Empty;
            StatusMessage = "Starting capture...";

            var success = await _audioCaptureService.StartCaptureAsync();
            if (!success)
            {
                ErrorMessage = "Failed to start audio capture";
                StatusMessage = "Capture failed";
            }
        }

        private async Task StopCaptureAsync()
        {
            StatusMessage = "Stopping capture...";
            await _audioCaptureService.StopCaptureAsync();
            StatusMessage = "Capture stopped";
            IsCapturing = false;
        }

        private async Task RequestPermissionAsync()
        {
            StatusMessage = "Requesting permission...";
            var result = await _permissionService.RequestMicrophonePermissionAsync();
            HasPermission = result == PermissionResult.Granted;

            StatusMessage = HasPermission
                ? "Permission granted - ready to capture"
                : "Permission denied - cannot capture audio";
        }

        private async Task LoadStringSetsAsync()
        {
            try
            {
                _allStringSets = await _stringSetRepository.GetAllAsync();

                // Populate StringSets (all sets)
                StringSets.Clear();
                foreach (var set in _allStringSets)
                {
                    StringSets.Add(set);
                }

                // Extract unique brands and populate AvailableBrands
                var brands = _allStringSets
                    .Select(s => s.Brand)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                AvailableBrands.Clear();
                foreach (var brand in brands)
                {
                    AvailableBrands.Add(brand);
                }

                // Auto-select first brand if available
                if (AvailableBrands.Count > 0 && SelectedBrand == null)
                {
                    SelectedBrand = AvailableBrands[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load string sets: {ex.Message}";
            }
        }

        /// <summary>
        /// Filters the available models based on the selected brand.
        /// </summary>
        private void FilterModelsByBrand()
        {
            FilteredModels.Clear();

            if (string.IsNullOrEmpty(_selectedBrand))
                return;

            var filtered = _allStringSets
                .Where(s => s.Brand == _selectedBrand)
                .OrderBy(s => s.Model)
                .ToList();

            foreach (var model in filtered)
            {
                FilteredModels.Add(model);
            }

            // Auto-select first model if available
            if (FilteredModels.Count > 0)
            {
                SelectedStringSet = FilteredModels[0];
            }
        }

        private async Task LoadBaselineForStringAsync()
        {
            if (SelectedStringSet == null)
            {
                SelectedBaseline = null;
                return;
            }

            try
            {
                SelectedBaseline = await _baselineRepository.GetBySetIdAndStringNumberAsync(
                    SelectedStringSet.Id,
                    SelectedStringNumber);

                // Load decay history and recommendation if baseline exists
                if (SelectedBaseline != null)
                {
                    await LoadDecayHistoryAsync(SelectedBaseline.Id);
                    await RefreshRecommendationAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load baseline: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates the baseline status for all 6 strings of the current string set.
        /// </summary>
        private async Task UpdateAllBaselineStatusAsync()
        {
            if (SelectedStringSet == null)
            {
                // Clear all status indicators
                foreach (var item in StringNumbers)
                {
                    item.HasBaseline = false;
                }
                return;
            }

            try
            {
                // Load all baselines for the current string set
                var baselines = await _baselineRepository.GetBySetIdAsync(SelectedStringSet.Id);

                // Update each string number's baseline status
                foreach (var item in StringNumbers)
                {
                    var baseline = baselines.FirstOrDefault(b => b.StringNumber == item.Number);
                    item.HasBaseline = baseline?.InitialCentroid > 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to check baseline status: {ex.Message}";
            }
        }

        private async Task LoadDecayHistoryAsync(int baselineId)
        {
            try
            {
                var history = await _measurementService.GetDecayTrendAsync(baselineId, 20);
                DecayHistory.Clear();
                foreach (var log in history)
                {
                    DecayHistory.Add(log);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load history: {ex.Message}";
            }
        }

        private async Task RefreshRecommendationAsync()
        {
            if (SelectedBaseline == null)
            {
                Recommendation = null;
                return;
            }

            IsLoadingRecommendation = true;

            try
            {
                Recommendation = await _recommendationService.AnalyzeDecayTrendAsync(SelectedBaseline.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load recommendation: {ex.Message}";
                Recommendation = null;
            }
            finally
            {
                IsLoadingRecommendation = false;
            }
        }

        private Task EstablishBaselineAsync()
        {
            if (SelectedBaseline == null)
            {
                ErrorMessage = "No baseline selected";
                return Task.CompletedTask;
            }

            StatusMessage = "Play the string to capture baseline...";
            // Note: The actual baseline capture happens in OnBufferCaptured
            // when the user plays the string. This method sets the flag.
            _isEstablishingBaseline = true;
            return Task.CompletedTask;
        }

        private bool _isEstablishingBaseline;

        private async Task NavigateToStringInputAsync()
        {
            await Shell.Current.GoToAsync("StringInputPage");
        }

        private async Task NavigateToGuitarInputAsync()
        {
            await Shell.Current.GoToAsync("GuitarInputPage");
        }

        private async Task NavigateToPairingsAsync()
        {
            await Shell.Current.GoToAsync("PairingsManagementPage");
        }

        private async Task NavigateToLibraryAsync()
        {
            await Shell.Current.GoToAsync("LibraryPage");
        }

        private void ToggleContextExpanded()
        {
            IsContextExpanded = !IsContextExpanded;
        }

        private async Task NavigateToChartAsync()
        {
            if (SelectedBaseline == null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"DecayChartPage?baselineId={SelectedBaseline.Id}");
        }

        private async Task LoadGuitarsAsync()
        {
            try
            {
                var guitars = await _guitarRepository.GetAllAsync();

                Guitars.Clear();
                foreach (var guitar in guitars)
                {
                    Guitars.Add(guitar);
                }

                // Auto-select first guitar if available
                if (Guitars.Count > 0 && SelectedGuitar == null)
                {
                    SelectedGuitar = Guitars[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load guitars: {ex.Message}";
            }
        }

        private async Task LoadPairingsForGuitarAsync()
        {
            AvailablePairings.Clear();
            _selectedPairingItem = null;
            OnPropertyChanged(nameof(SelectedPairing));
            OnPropertyChanged(nameof(HasPairings));

            if (SelectedGuitar == null)
            {
                return;
            }

            try
            {
                var pairings = await _pairingRepository.GetByGuitarIdAsync(SelectedGuitar.Id);

                foreach (var pairing in pairings)
                {
                    // Find the associated string set for display
                    var stringSet = _allStringSets.FirstOrDefault(s => s.Id == pairing.SetId);
                    if (stringSet != null)
                    {
                        AvailablePairings.Add(new PairingDisplayItem(pairing, stringSet));
                    }
                }

                OnPropertyChanged(nameof(HasPairings));

                // Auto-select the active pairing, or the first one if none is active
                var activeDisplayItem = AvailablePairings.FirstOrDefault(p => p.IsActive) ?? AvailablePairings.FirstOrDefault();
                if (activeDisplayItem != null)
                {
                    _selectedPairingItem = activeDisplayItem;
                    OnPropertyChanged(nameof(SelectedPairing));
                    await LoadStringSetFromPairingAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load guitar's string sets: {ex.Message}";
            }
        }

        private Task LoadStringSetFromPairingAsync()
        {
            if (_selectedPairingItem == null)
            {
                SelectedBrand = null;
                SelectedStringSet = null;
                return Task.CompletedTask;
            }

            try
            {
                var stringSet = _selectedPairingItem.StringSet;
                // Set brand and model to load the correct baseline
                SelectedBrand = stringSet.Brand;
                SelectedStringSet = stringSet;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load string set: {ex.Message}";
            }

            return Task.CompletedTask;
        }

        private async Task DeleteSelectedPairingAsync()
        {
            if (_selectedPairingItem == null)
            {
                ErrorMessage = "No pairing selected";
                return;
            }

            try
            {
                var pairingId = _selectedPairingItem.Pairing.Id;

                // Delete the pairing
                await _pairingRepository.DeleteAsync(pairingId);

                StatusMessage = "String set pairing deleted";

                // Reload pairings for the guitar
                await LoadPairingsForGuitarAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete pairing: {ex.Message}";
            }
        }

        private void OnBufferCaptured(object? sender, AudioBufferCapturedEventArgs e)
        {
            // Update RMS level on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RmsLevel = e.Buffer.RmsAmplitude;
            });

            // Only process if threshold exceeded
            if (!e.ThresholdExceeded)
            {
                return;
            }

            // Run analysis on background thread
            _ = ProcessBufferAsync(e.Buffer);
        }

        private async Task ProcessBufferAsync(AudioBuffer buffer)
        {
            if (SelectedBaseline == null)
            {
                return;
            }

            IsAnalyzing = true;

            try
            {
                if (_isEstablishingBaseline)
                {
                    // Establish new baseline
                    var success = await _measurementService.EstablishBaselineAsync(
                        buffer,
                        SelectedBaseline.Id);

                    if (success)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            StatusMessage = "Baseline established successfully";
                            _isEstablishingBaseline = false;
                            await LoadBaselineForStringAsync();
                            await UpdateAllBaselineStatusAsync();
                        });
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ErrorMessage = "Failed to establish baseline";
                            _isEstablishingBaseline = false;
                        });
                    }
                }
                else
                {
                    // Regular measurement
                    var result = await _measurementService.MeasureAsync(
                        buffer,
                        SelectedBaseline.Id);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (result.Success && result.AnalysisResult != null)
                        {
                            CurrentCentroid = result.AnalysisResult.SpectralCentroid;
                            CurrentHfRatio = result.AnalysisResult.HfEnergyRatio;
                            FundamentalFreq = result.AnalysisResult.FundamentalFreq;
                            DecayPercentage = result.AnalysisResult.DecayPercentage ?? 0;

                            StatusMessage = $"Analysis: {CurrentCentroid:F0} Hz centroid";

                            // Add to history
                            if (result.Log != null)
                            {
                                DecayHistory.Insert(0, result.Log);
                                if (DecayHistory.Count > 20)
                                {
                                    DecayHistory.RemoveAt(DecayHistory.Count - 1);
                                }

                                // Refresh recommendation with new data
                                _ = RefreshRecommendationAsync();
                            }
                        }
                        else
                        {
                            ErrorMessage = result.ErrorMessage ?? "Analysis failed";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorMessage = $"Analysis error: {ex.Message}";
                });
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private void OnCaptureStateChanged(object? sender, AudioCaptureState state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsCapturing = state == AudioCaptureState.Capturing;
                StatusMessage = state switch
                {
                    AudioCaptureState.Stopped => "Capture stopped",
                    AudioCaptureState.Initializing => "Initializing...",
                    AudioCaptureState.Capturing => "Capturing - play string to analyze",
                    AudioCaptureState.Error => "Capture error",
                    _ => StatusMessage
                };
            });
        }

        private void OnCaptureError(object? sender, AudioCaptureErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ErrorMessage = e.Message;
                StatusMessage = "Capture error occurred";
            });
        }

        private void UpdateHealthIndicator()
        {
            // Color-coded health based on decay percentage
            // Green: 0-15% decay (excellent)
            // Yellow: 15-30% decay (good)
            // Orange: 30-50% decay (fair)
            // Red: 50%+ decay (replace recommended)

            if (DecayPercentage < 15)
            {
                HealthStatus = "Excellent";
                HealthColor = Color.FromArgb("#22c55e"); // Green
            }
            else if (DecayPercentage < 30)
            {
                HealthStatus = "Good";
                HealthColor = Color.FromArgb("#eab308"); // Yellow
            }
            else if (DecayPercentage < 50)
            {
                HealthStatus = "Fair";
                HealthColor = Color.FromArgb("#f97316"); // Orange
            }
            else
            {
                HealthStatus = "Replace";
                HealthColor = Color.FromArgb("#ef4444"); // Red
            }
        }

        /// <summary>
        /// Determines whether audio capture can be started based on current selection state.
        /// Requires brand, model, and string number to be selected.
        /// </summary>
        /// <returns>True if all required selections are made; otherwise, false.</returns>
        private bool CanStartCapture()
        {
            return !string.IsNullOrEmpty(SelectedBrand)
                && SelectedStringSet != null
                && SelectedStringNumber >= 1
                && SelectedStringNumber <= 6;
        }

        private void UpdateCommandStates()
        {
            (StartCaptureCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (StopCaptureCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RequestPermissionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (EstablishBaselineCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (NavigateToChartCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DeletePairingCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes resources used by the ViewModel.
        /// </summary>
        public void Dispose()
        {
            _audioCaptureService.BufferCaptured -= OnBufferCaptured;
            _audioCaptureService.StateChanged -= OnCaptureStateChanged;
            _audioCaptureService.ErrorOccurred -= OnCaptureError;
        }

        #endregion
    }

    /// <summary>
    /// Represents a string number option for selection.
    /// </summary>
    public class StringNumberItem : INotifyPropertyChanged
    {
        private bool _hasBaseline;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the string number (1-6).
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// Gets the display name for the string.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the short display name for the string (just the note).
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Gets or sets whether this string has a baseline established.
        /// </summary>
        public bool HasBaseline
        {
            get => _hasBaseline;
            set
            {
                if (_hasBaseline != value)
                {
                    _hasBaseline = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasBaseline)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusIcon)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor)));
                }
            }
        }

        /// <summary>
        /// Gets the status icon based on baseline state.
        /// </summary>
        public string StatusIcon => HasBaseline ? "✓" : "○";

        /// <summary>
        /// Gets the status color based on baseline state.
        /// </summary>
        public Color StatusColor => HasBaseline
            ? Color.FromArgb("#22c55e")  // Green for established
            : Color.FromArgb("#9ca3af"); // Gray for not set

        /// <summary>
        /// Initializes a new instance of StringNumberItem.
        /// </summary>
        public StringNumberItem(int number, string displayName)
        {
            Number = number;
            DisplayName = displayName;
            ShortName = displayName.Split(' ')[0]; // e.g., "High" from "High E (1)"
        }

        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Represents a guitar-string set pairing for display in selection controls.
    /// Combines pairing metadata with string set information for user-friendly presentation.
    /// </summary>
    public class PairingDisplayItem
    {
        /// <summary>
        /// Gets the underlying pairing entity.
        /// </summary>
        public GuitarStringSetPairing Pairing { get; }

        /// <summary>
        /// Gets the associated string set.
        /// </summary>
        public StringSet StringSet { get; }

        /// <summary>
        /// Gets the display name combining brand, model, and installation date.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the installation date formatted for display.
        /// </summary>
        public string InstalledDateText { get; }

        /// <summary>
        /// Gets whether this pairing is currently active.
        /// </summary>
        public bool IsActive => Pairing.IsActive;

        /// <summary>
        /// Initializes a new instance of PairingDisplayItem.
        /// </summary>
        public PairingDisplayItem(GuitarStringSetPairing pairing, StringSet stringSet)
        {
            Pairing = pairing;
            StringSet = stringSet;
            InstalledDateText = pairing.InstalledAt.ToString("MMM d, yyyy");

            var activeIndicator = pairing.IsActive ? " (Active)" : "";
            DisplayName = $"{stringSet.Brand} {stringSet.Model} - {InstalledDateText}{activeIndicator}";
        }

        /// <inheritdoc />
        public override string ToString() => DisplayName;
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.ViewModels
{
    /// <summary>
    /// ViewModel for the decay trend chart page.
    /// Displays time-series decay data with user-selectable metrics.
    /// </summary>
    public class DecayChartViewModel : BaseViewModel
    {
        private readonly IMeasurementLogRepository _measurementLogRepository;
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly IStringSetRepository _stringSetRepository;

        private int? _baselineId;
        private string _chartSubtitle = string.Empty;
        private bool _showDecayPercentage = true;
        private bool _showSpectralCentroid;
        private bool _showHfRatio;
        private bool _hasData;

        // Series data collections
        private readonly ObservableCollection<DateTimePoint> _decayData = new();
        private readonly ObservableCollection<DateTimePoint> _centroidData = new();
        private readonly ObservableCollection<DateTimePoint> _hfRatioData = new();

        /// <summary>
        /// Initializes a new instance of the DecayChartViewModel class.
        /// </summary>
        public DecayChartViewModel(
            IMeasurementLogRepository measurementLogRepository,
            IStringBaselineRepository baselineRepository,
            IStringSetRepository stringSetRepository)
        {
            _measurementLogRepository = measurementLogRepository ?? throw new ArgumentNullException(nameof(measurementLogRepository));
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
            _stringSetRepository = stringSetRepository ?? throw new ArgumentNullException(nameof(stringSetRepository));

            Title = "Decay Trend";

            // Initialize commands
            ToggleDecayCommand = new RelayCommand(ToggleDecay);
            ToggleCentroidCommand = new RelayCommand(ToggleCentroid);
            ToggleHfRatioCommand = new RelayCommand(ToggleHfRatio);
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            GoBackCommand = new AsyncRelayCommand(GoBackAsync);

            // Initialize chart series
            InitializeChartSeries();
        }

        #region Properties

        /// <summary>
        /// Gets the context subtitle showing which guitar string / set is being charted.
        /// </summary>
        public string ChartSubtitle
        {
            get => _chartSubtitle;
            private set => SetProperty(ref _chartSubtitle, value);
        }

        /// <summary>
        /// Gets or sets whether to show decay percentage on the chart.
        /// </summary>
        public bool ShowDecayPercentage
        {
            get => _showDecayPercentage;
            set
            {
                if (SetProperty(ref _showDecayPercentage, value))
                {
                    UpdateSeriesVisibility();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show spectral centroid on the chart.
        /// </summary>
        public bool ShowSpectralCentroid
        {
            get => _showSpectralCentroid;
            set
            {
                if (SetProperty(ref _showSpectralCentroid, value))
                {
                    UpdateSeriesVisibility();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show HF energy ratio on the chart.
        /// </summary>
        public bool ShowHfRatio
        {
            get => _showHfRatio;
            set
            {
                if (SetProperty(ref _showHfRatio, value))
                {
                    UpdateSeriesVisibility();
                }
            }
        }

        /// <summary>
        /// Gets the chart series collection.
        /// </summary>
        public ObservableCollection<ISeries> Series { get; } = new();

        /// <summary>
        /// Gets the X-axis configuration (time axis).
        /// </summary>
        public Axis[] XAxes { get; } = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM dd"))
            {
                Name = "Date",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12
            }
        };

        /// <summary>
        /// Gets the Y-axis configuration.
        /// </summary>
        public Axis[] YAxes { get; private set; } = new Axis[]
        {
            new Axis
            {
                Name = "Decay (%)",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12,
                MinLimit = 0,
                MaxLimit = 100
            }
        };

        /// <summary>
        /// Gets the baseline ID being analyzed.
        /// </summary>
        public int? BaselineId => _baselineId;

        /// <summary>
        /// Gets a value indicating whether there is data to display.
        /// </summary>
        public bool HasData
        {
            get => _hasData;
            private set => SetProperty(ref _hasData, value);
        }

        /// <summary>
        /// Gets a value indicating whether there is no data to display.
        /// </summary>
        public bool HasNoData => !HasData;

        #endregion

        #region Commands

        /// <summary>
        /// Command to toggle decay percentage visibility.
        /// </summary>
        public ICommand ToggleDecayCommand { get; }

        /// <summary>
        /// Command to toggle spectral centroid visibility.
        /// </summary>
        public ICommand ToggleCentroidCommand { get; }

        /// <summary>
        /// Command to toggle HF ratio visibility.
        /// </summary>
        public ICommand ToggleHfRatioCommand { get; }

        /// <summary>
        /// Command to refresh the chart data.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to navigate back.
        /// </summary>
        public ICommand GoBackCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the chart with data for the specified baseline.
        /// </summary>
        /// <param name="baselineId">The baseline ID to load data for.</param>
        public async Task InitializeAsync(int baselineId)
        {
            _baselineId = baselineId;
            await LoadContextSubtitleAsync(baselineId);
            await LoadDataAsync();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Maps string number (1-6) to note name.
        /// </summary>
        private static readonly string[] StringNoteNames = { "E1", "B2", "G3", "D4", "A5", "E6" };

        private async Task LoadContextSubtitleAsync(int baselineId)
        {
            try
            {
                var baseline = await _baselineRepository.GetByIdAsync(baselineId);
                if (baseline == null) return;

                var stringSet = await _stringSetRepository.GetByIdAsync(baseline.SetId);
                if (stringSet == null) return;

                var noteName = baseline.StringNumber >= 1 && baseline.StringNumber <= 6
                    ? StringNoteNames[baseline.StringNumber - 1]
                    : $"#{baseline.StringNumber}";

                ChartSubtitle = $"{stringSet.Brand} {stringSet.Model} - String {baseline.StringNumber} ({noteName})";
            }
            catch
            {
                // Non-critical, leave subtitle empty
            }
        }

        private void InitializeChartSeries()
        {
            // Decay percentage series (primary - always visible by default)
            var decaySeries = new LineSeries<DateTimePoint>
            {
                Name = "Decay %",
                Values = _decayData,
                Stroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                IsVisible = true
            };

            // Spectral centroid series (secondary)
            var centroidSeries = new LineSeries<DateTimePoint>
            {
                Name = "Centroid (Hz/10)",
                Values = _centroidData,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                IsVisible = false
            };

            // HF ratio series (secondary)
            var hfRatioSeries = new LineSeries<DateTimePoint>
            {
                Name = "HF Ratio x100",
                Values = _hfRatioData,
                Stroke = new SolidColorPaint(SKColors.MediumPurple, 2),
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(SKColors.MediumPurple, 2),
                IsVisible = false
            };

            Series.Add(decaySeries);
            Series.Add(centroidSeries);
            Series.Add(hfRatioSeries);
        }

        private async Task LoadDataAsync()
        {
            if (_baselineId == null)
            {
                HasData = false;
                OnPropertyChanged(nameof(HasNoData));
                return;
            }

            IsBusy = true;

            try
            {
                var measurements = await _measurementLogRepository.GetByBaselineIdAsync(_baselineId.Value);

                _decayData.Clear();
                _centroidData.Clear();
                _hfRatioData.Clear();

                foreach (var log in measurements.OrderBy(m => m.MeasuredAt))
                {
                    _decayData.Add(new DateTimePoint(log.MeasuredAt, log.DecayPercentage));
                    // Scale centroid for better visualization (divide by 10 to fit on same scale)
                    _centroidData.Add(new DateTimePoint(log.MeasuredAt, log.CurrentCentroid / 10));
                    // Scale HF ratio for better visualization (multiply by 100)
                    _hfRatioData.Add(new DateTimePoint(log.MeasuredAt, log.CurrentHighRatio * 100));
                }

                HasData = _decayData.Count > 0;
                OnPropertyChanged(nameof(HasNoData));
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load chart data: {ex.Message}");
                HasData = false;
                OnPropertyChanged(nameof(HasNoData));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ToggleDecay()
        {
            ShowDecayPercentage = !ShowDecayPercentage;
        }

        private void ToggleCentroid()
        {
            ShowSpectralCentroid = !ShowSpectralCentroid;
        }

        private void ToggleHfRatio()
        {
            ShowHfRatio = !ShowHfRatio;
        }

        private void UpdateSeriesVisibility()
        {
            if (Series.Count >= 3)
            {
                Series[0].IsVisible = ShowDecayPercentage;
                Series[1].IsVisible = ShowSpectralCentroid;
                Series[2].IsVisible = ShowHfRatio;
            }

            // Update Y-axis based on visible series
            UpdateYAxis();
        }

        private void UpdateYAxis()
        {
            string name;
            double? maxLimit = null;

            if (ShowDecayPercentage && !ShowSpectralCentroid && !ShowHfRatio)
            {
                name = "Decay (%)";
                maxLimit = 100;
            }
            else if (!ShowDecayPercentage && ShowSpectralCentroid && !ShowHfRatio)
            {
                name = "Centroid (Hz/10)";
                maxLimit = null; // Auto-scale
            }
            else if (!ShowDecayPercentage && !ShowSpectralCentroid && ShowHfRatio)
            {
                name = "HF Ratio x100";
                maxLimit = null;
            }
            else
            {
                name = "Value";
                maxLimit = null;
            }

            YAxes[0] = new Axis
            {
                Name = name,
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12,
                MinLimit = 0,
                MaxLimit = maxLimit
            };

            OnPropertyChanged(nameof(YAxes));
        }

        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        #endregion
    }
}

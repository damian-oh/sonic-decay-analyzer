using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Full-screen decay trend chart page.
    /// </summary>
    [QueryProperty(nameof(BaselineId), "baselineId")]
    [QueryProperty(nameof(SetId), "setId")]
    public partial class DecayChartPage : ContentPage
    {
        private readonly DecayChartViewModel _viewModel;

        /// <summary>
        /// Gets or sets the baseline ID passed via navigation (query string).
        /// </summary>
        public string? BaselineId { get; set; }

        /// <summary>
        /// Gets or sets the string set ID passed via navigation (query string).
        /// </summary>
        public string? SetId { get; set; }

        /// <summary>
        /// Initializes a new instance of the DecayChartPage class.
        /// </summary>
        /// <param name="viewModel">The ViewModel for this page.</param>
        public DecayChartPage(DecayChartViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!string.IsNullOrEmpty(BaselineId) && int.TryParse(BaselineId, out int baselineId))
            {
                if (!string.IsNullOrEmpty(SetId) && int.TryParse(SetId, out int setId))
                {
                    await _viewModel.InitializeAsync(baselineId, setId);
                }
                else
                {
                    await _viewModel.InitializeAsync(baselineId);
                }
            }
        }
    }
}

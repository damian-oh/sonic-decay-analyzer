using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Full-screen decay trend chart page.
    /// </summary>
    [QueryProperty(nameof(BaselineId), "baselineId")]
    public partial class DecayChartPage : ContentPage
    {
        private readonly DecayChartViewModel _viewModel;

        /// <summary>
        /// Gets or sets the baseline ID passed via navigation (query string).
        /// </summary>
        public string? BaselineId { get; set; }

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
            if (!string.IsNullOrEmpty(BaselineId) && int.TryParse(BaselineId, out int id))
            {
                await _viewModel.InitializeAsync(id);
            }
        }
    }
}

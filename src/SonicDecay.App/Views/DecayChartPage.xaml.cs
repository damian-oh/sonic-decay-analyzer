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
        private int _baselineId;

        /// <summary>
        /// Gets or sets the baseline ID passed via navigation.
        /// </summary>
        public int BaselineId
        {
            get => _baselineId;
            set
            {
                _baselineId = value;
                _ = _viewModel.InitializeAsync(value);
            }
        }

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
    }
}

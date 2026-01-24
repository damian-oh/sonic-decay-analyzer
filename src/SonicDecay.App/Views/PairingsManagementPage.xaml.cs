using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page for managing guitar-string set pairings.
    /// </summary>
    public partial class PairingsManagementPage : ContentPage
    {
        private readonly PairingsManagementViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the PairingsManagementPage class.
        /// </summary>
        public PairingsManagementPage(PairingsManagementViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <summary>
        /// Called when the page is appearing.
        /// Initializes the ViewModel.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}

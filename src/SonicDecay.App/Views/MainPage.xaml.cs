using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Main page for spectral analysis and decay monitoring.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        /// <param name="viewModel">The MainViewModel instance from DI.</param>
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        /// <inheritdoc />
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            // Clean up when page is navigated away from
            // ViewModel will re-subscribe to events in InitializeAsync on next appearance
            await _viewModel.CleanupAsync();
        }
    }
}

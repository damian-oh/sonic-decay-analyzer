using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page for creating and editing guitars.
    /// </summary>
    public partial class GuitarInputPage : ContentPage
    {
        private readonly GuitarInputViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the GuitarInputPage class.
        /// </summary>
        /// <param name="viewModel">The ViewModel for this page.</param>
        public GuitarInputPage(GuitarInputViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <summary>
        /// Called when the page appears.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.Reset();
            await _viewModel.InitializeAsync();
        }
    }
}

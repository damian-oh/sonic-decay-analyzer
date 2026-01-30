using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page displaying the list of guitars with navigation to add/edit.
    /// </summary>
    public partial class GuitarsListPage : ContentPage
    {
        private readonly GuitarsListViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the GuitarsListPage class.
        /// </summary>
        /// <param name="viewModel">The GuitarsListViewModel instance from DI.</param>
        public GuitarsListPage(GuitarsListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadGuitarsAsync();
        }
    }
}

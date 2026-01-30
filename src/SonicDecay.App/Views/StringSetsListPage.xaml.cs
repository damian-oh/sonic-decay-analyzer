using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page displaying the list of string sets with navigation to add/edit.
    /// </summary>
    public partial class StringSetsListPage : ContentPage
    {
        private readonly StringSetsListViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the StringSetsListPage class.
        /// </summary>
        /// <param name="viewModel">The StringSetsListViewModel instance from DI.</param>
        public StringSetsListPage(StringSetsListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadStringSetsAsync();
        }
    }
}

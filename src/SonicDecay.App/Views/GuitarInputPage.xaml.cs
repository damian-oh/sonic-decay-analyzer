using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page for creating and editing guitars.
    /// </summary>
    [QueryProperty(nameof(GuitarId), "guitarId")]
    public partial class GuitarInputPage : ContentPage
    {
        private readonly GuitarInputViewModel _viewModel;

        /// <summary>
        /// Gets or sets the guitar ID for editing (passed via navigation).
        /// </summary>
        public string? GuitarId { get; set; }

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

            // Initialize first to load string sets, then load guitar data if editing
            // This order ensures AvailableStringSets is populated before LoadForEditAsync accesses it
            await _viewModel.InitializeAsync();

            if (!string.IsNullOrEmpty(GuitarId) && int.TryParse(GuitarId, out int id))
            {
                await _viewModel.LoadForEditAsync(id);
            }
            else
            {
                _viewModel.Reset();
            }
        }
    }
}

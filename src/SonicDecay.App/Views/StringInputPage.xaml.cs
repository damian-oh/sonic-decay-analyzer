using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Page for creating and editing string sets.
    /// </summary>
    [QueryProperty(nameof(StringSetId), "id")]
    public partial class StringInputPage : ContentPage
    {
        private readonly StringInputViewModel _viewModel;

        /// <summary>
        /// Gets or sets the string set ID for editing (passed via navigation).
        /// </summary>
        public string? StringSetId { get; set; }

        /// <summary>
        /// Initializes a new instance of the StringInputPage class.
        /// </summary>
        /// <param name="viewModel">The StringInputViewModel instance from DI.</param>
        public StringInputPage(StringInputViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!string.IsNullOrEmpty(StringSetId) && int.TryParse(StringSetId, out int id))
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

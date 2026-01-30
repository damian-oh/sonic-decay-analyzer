using SonicDecay.App.ViewModels;

namespace SonicDecay.App.Views
{
    /// <summary>
    /// Library hub page for navigating to Guitars, String Sets, and Pairings.
    /// </summary>
    public partial class LibraryPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the LibraryPage class.
        /// </summary>
        /// <param name="viewModel">The LibraryViewModel instance from DI.</param>
        public LibraryPage(LibraryViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

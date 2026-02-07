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

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

        /// <summary>
        /// Animates the context section expand/collapse transition.
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsContextExpanded) && ContextContent != null)
            {
                if (_viewModel.IsContextExpanded)
                {
                    ContextContent.IsVisible = true;
                    ContextContent.Opacity = 0;
                    ContextContent.TranslationY = -10;
                    ContextContent.FadeTo(1, 200, Easing.CubicOut);
                    ContextContent.TranslateTo(0, 0, 200, Easing.CubicOut);
                }
                else
                {
                    ContextContent.FadeTo(0, 150, Easing.CubicIn);
                    ContextContent.TranslateTo(0, -10, 150, Easing.CubicIn).ContinueWith(_ =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ContextContent.IsVisible = false;
                        });
                    });
                }
            }
        }
    }
}

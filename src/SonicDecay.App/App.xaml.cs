using System.Diagnostics;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App
{
    /// <summary>
    /// Main application class for Sonic Decay Analyzer.
    /// Initializes the app shell and seeds database with preset string sets.
    /// </summary>
    public partial class App : Application
    {
        private readonly ISeedDataService _seedDataService;

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        /// <param name="seedDataService">The seed data service for database initialization.</param>
        public App(ISeedDataService seedDataService)
        {
            InitializeComponent();
            _seedDataService = seedDataService ?? throw new ArgumentNullException(nameof(seedDataService));
        }

        /// <inheritdoc />
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        /// <inheritdoc />
        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                await _seedDataService.SeedIfEmptyAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app - seed data is not critical for startup
                Debug.WriteLine($"[App] Failed to seed database: {ex.Message}");
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SonicDecay.App.Services.Implementations;
using SonicDecay.App.Services.Interfaces;
using SonicDecay.App.ViewModels;
using SonicDecay.App.Views;

namespace SonicDecay.App
{
    /// <summary>
    /// MAUI application builder and dependency injection configuration.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI application.
        /// </summary>
        /// <returns>The configured MauiApp instance.</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register database services
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

            // Register repositories (order matters for dependency resolution)
            builder.Services.AddTransient<IMeasurementLogRepository, MeasurementLogRepository>();
            builder.Services.AddTransient<IStringBaselineRepository, StringBaselineRepository>();
            builder.Services.AddTransient<IGuitarStringSetPairingRepository, GuitarStringSetPairingRepository>();
            builder.Services.AddTransient<IStringSetRepository, StringSetRepository>();
            builder.Services.AddTransient<IGuitarRepository, GuitarRepository>();

            // Register seed data service for preset string sets
            builder.Services.AddSingleton<ISeedDataService, SeedDataService>();

            // Register audio capture services
            builder.Services.AddSingleton<IPermissionService, PermissionService>();
            builder.Services.AddSingleton<IAudioCaptureService, AudioCaptureService>();

            // Register spectral analysis service (native C# implementation)
            // Cross-platform compatible: iOS, Android, Windows, macOS
            builder.Services.AddSingleton<IAnalysisService, NativeAnalysisService>();

            // Register measurement coordination service (analysis + persistence)
            builder.Services.AddTransient<IMeasurementService, MeasurementService>();

            // Register predictive maintenance services (Phase 5)
            builder.Services.AddTransient<IRecommendationService, RecommendationService>();
#if !ANDROID && !IOS && !MACCATALYST
            builder.Services.AddSingleton<IPythonEnginePool, PythonEnginePool>();
#endif

            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<StringInputViewModel>();
            builder.Services.AddTransient<GuitarInputViewModel>();
            builder.Services.AddTransient<DecayChartViewModel>();
            builder.Services.AddTransient<PairingsManagementViewModel>();

            // Register Views (with ViewModel injection)
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StringInputPage>();
            builder.Services.AddTransient<GuitarInputPage>();
            builder.Services.AddTransient<DecayChartPage>();
            builder.Services.AddTransient<PairingsManagementPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

using Microsoft.Extensions.Logging;
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
            builder.Services.AddTransient<IStringSetRepository, StringSetRepository>();

            // Register audio capture services
            builder.Services.AddSingleton<IPermissionService, PermissionService>();
            builder.Services.AddSingleton<IAudioCaptureService, AudioCaptureService>();

            // Register spectral analysis service (Python engine interop)
            builder.Services.AddSingleton<IAnalysisService, AnalysisService>();

            // Register measurement coordination service (analysis + persistence)
            builder.Services.AddTransient<IMeasurementService, MeasurementService>();

            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<StringInputViewModel>();

            // Register Views (with ViewModel injection)
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StringInputPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

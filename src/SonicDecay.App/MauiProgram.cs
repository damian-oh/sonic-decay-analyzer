using Microsoft.Extensions.Logging;
using SonicDecay.App.Services.Implementations;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App
{
    public static class MauiProgram
    {
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

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

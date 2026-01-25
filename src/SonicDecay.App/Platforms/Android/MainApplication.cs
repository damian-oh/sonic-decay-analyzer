using Android.App;
using Android.Runtime;

namespace SonicDecay.App
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // Register global exception handlers for debugging startup crashes
            AndroidEnvironment.UnhandledExceptionRaiser += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        protected override MauiApp CreateMauiApp()
        {
            try
            {
                Android.Util.Log.Info("SonicDecay", "Creating MauiApp...");
                var app = MauiProgram.CreateMauiApp();
                Android.Util.Log.Info("SonicDecay", "MauiApp created successfully");
                return app;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("SonicDecay", $"Failed to create MauiApp: {ex}");
                throw;
            }
        }

        private void OnUnhandledException(object? sender, RaiseThrowableEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[CRASH] AndroidEnvironment: {e.Exception}");
            Android.Util.Log.Error("SonicDecay", $"Unhandled exception: {e.Exception}");
        }

        private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[CRASH] AppDomain: {ex}");
            Android.Util.Log.Error("SonicDecay", $"Domain unhandled exception: {ex}");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[CRASH] UnobservedTask: {e.Exception}");
            Android.Util.Log.Error("SonicDecay", $"Unobserved task exception: {e.Exception}");
        }
    }
}

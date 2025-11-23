using interpreter.Maui.ServiceDefaults;
using interpreter.Maui.Services;
using Microsoft.Extensions.Logging;

namespace interpreter.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
#if DEBUG
            // Prevent ANR during debugger attachment
            System.Diagnostics.Debug.WriteLine("MauiProgram: Starting app creation...");
#endif

            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp();
            builder.AddServiceDefaults();

            // Platform services
            builder.Services.AddSingleton<IAudioRecordingService, AndroidAudioRecordingService>();
            builder.Services.AddSingleton<IAudioPlaybackService, AndroidAudioPlaybackService>();

#if DEBUG
            // AddDebug() can cause ANR during debugger attachment on Android
            // Use filtered logging to reduce overhead
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
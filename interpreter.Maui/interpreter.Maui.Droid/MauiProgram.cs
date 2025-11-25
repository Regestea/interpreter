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
            builder.Services.AddSingleton<IAdjustmentService, AdjustmentService>();
            
            // Audio calibration
            builder.Services.AddSingleton<AudioCalibration>();
            
            // Audio recording infrastructure services
            // Note: These are registered for testability and consistency,
            // but RecordingForegroundService may instantiate them directly
            // since Android Services are created by the system, not the DI container
            builder.Services.AddSingleton<AudioRecordingConfiguration>();
            builder.Services.AddTransient<IAudioRecorder, AudioRecorder>();
            // RecordingNotificationManager requires Service instance in constructor,
            // so it cannot be injected in RecordingForegroundService via DI

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
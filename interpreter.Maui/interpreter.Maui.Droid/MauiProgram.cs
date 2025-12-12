using interpreter.Maui.ServiceDefaults;
using interpreter.Maui.Services;
using Microsoft.Extensions.Logging;
using Opus.Services;

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

            // Backend API base address for Android emulator -> host machine
            // Matches interpreter.Api launchSettings: http://localhost:5021
            builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://regestea.ir/");
            });

            // HttpClient used for auth; same base address
            builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.BaseAddress = new Uri("https://regestea.ir/");
            });
            

            // Platform services
            builder.Services.AddSingleton<IAndroidAudioRecordingService, AndroidAudioRecordingService>();
            builder.Services.AddSingleton<IAudioPlaybackService, AndroidAudioPlaybackService>();
            builder.Services.AddSingleton<IAdjustmentService, AdjustmentService>();
            builder.Services.AddScoped<IVoiceProfileService,VoiceProfileService>();
            // builder.Services.AddScoped<IOpusCodecService,OpusCodecService>();
            
            // Audio calibration
            builder.Services.AddSingleton<AudioCalibration>();
            
            // Audio recording infrastructure services
            // Note: These are registered for testability and consistency,
            // but RecordingForegroundService may instantiate them directly
            // since Android Services are created by the system, not the DI container
            builder.Services.AddSingleton<AudioRecordingConfiguration>();
            builder.Services.AddTransient<IAudioRecorderService, AudioRecorderService>();
            // RecordingNotificationManager requires Service instance in constructor,
            // so it cannot be injected in RecordingForegroundService via DI
            
            builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
            
            // Microphone management service
            builder.Services.AddSingleton<IMicrophoneManagerService>(_ =>
            {
                var context = Android.App.Application.Context;
                return new MicrophoneManagerService(context);
            });

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
using interpreter.Maui.ServiceDefaults;
using interpreter.Maui.Services;

namespace interpreter.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp();
            builder.AddServiceDefaults();

            // Platform services
            builder.Services.AddSingleton<IAudioRecordingService, AndroidAudioRecordingService>();
            builder.Services.AddSingleton<IAudioPlaybackService, AndroidAudioPlaybackService>();

            return builder.Build();
        }
    }
}

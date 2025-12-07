using Microsoft.Extensions.Logging;
using interpreter.Maui.Services;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Pages;

namespace interpreter.Maui
{
    public static class MauiProgramExtensions
    {
        public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
        {
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register shared services
            builder.Services.AddSingleton<IAnimationService, AnimationService>();
            builder.Services.AddSingleton<IButtonStateService, ButtonStateService>();
            builder.Services.AddSingleton<IModalService, ModalService>();
            
            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<RecordingViewModel>();
            builder.Services.AddTransient<VoiceProfilesViewModel>();
            builder.Services.AddTransient<MicrophoneManagerViewModel>();
            
            // Register Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<RecordingPage>();
            builder.Services.AddTransient<VoiceProfilesPage>();
            builder.Services.AddTransient<MicrophoneManagerPage>();

#if DEBUG
            // AddDebug() can cause ANR during debugger attachment on Android
            // Use filtered logging to reduce overhead
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.Logging.AddDebug();
#endif

            return builder;
        }
    }
}

﻿using Microsoft.Extensions.Logging;

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

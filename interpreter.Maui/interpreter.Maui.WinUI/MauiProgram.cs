using Microsoft.Extensions.Hosting;

namespace interpreter.Maui.WinUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp();
            builder.AddServiceDefaults();

            return builder.Build();
        }
    }
}

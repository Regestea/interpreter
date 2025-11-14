using interpreter.Maui.ServiceDefaults;

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

            return builder.Build();
        }
    }
}

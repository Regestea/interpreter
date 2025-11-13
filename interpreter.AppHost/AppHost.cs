var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder
    .AddProject<Projects.interpreter_Api>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.interpreter_Maui_WinUI>("mauiappwin")
    .WithEnvironment("TargetFramework", "net10.0-windows10.0.19041.0")
    .WithReference(apiService);

    builder.AddExecutable(
        "maui-android",
        "dotnet",
        "../interpreter.Maui/interpreter.Maui.Droid",
        "build",
        "interpreter.Maui.Droid.csproj",
        "-t:Run",
        "-f", "net10.0-android"
    ).WithReference(apiService);


builder.Build().Run();
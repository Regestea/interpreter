using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Extensions.DependencyInjection;
using interpreter.ServiceDefaults;
using interpreter.Api.Models;
using interpreter.Api.Services;
using interpreter.Api.Data;
using Opus.Services;
using PiperSharp;
using Microsoft.EntityFrameworkCore;
using SpeechBrain;

namespace interpreter.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Ensure Piper TTS is extracted before starting the application
            try
            {
                Console.WriteLine("Checking Piper TTS installation...");
                Console.WriteLine($"Application Base Directory: {AppContext.BaseDirectory}");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Looking for PiperData.zip at: {PiperDataExtractor.PiperDataZipPath}");

                PiperDataExtractor.EnsurePiperExtracted();
                Console.WriteLine("Piper TTS is ready.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to extract Piper TTS data: {ex.Message}");
                Console.WriteLine("Text-to-speech features may not be available.");
            }

            // Add services to the container.

            // Configure SQLite DbContext
            var dbPath = Path.Combine(AppContext.BaseDirectory, "interpreter.db");
            var connectionString = $"Data Source={dbPath}";

            builder.Services.AddDbContext<InterpreterDbContext>(options =>
                options.UseSqlite(connectionString));
            
            
            builder.Services.AddMemoryCache();
            
            builder.Services.AddDistributedMemoryCache();
            
            builder.Services.AddIdempotentAPI();
            
            builder.Services.AddIdempotentAPIUsingDistributedCache();

            // Register Cache Service for easy caching
            builder.Services.AddSingleton<ICacheService, CacheService>();

            builder.Services.AddSingleton<ISpeechBrainRecognition, SpeechBrainRecognition>(x =>
            {
                var service = new SpeechBrainRecognition();
                service.Initialize();
                return service;
            });

            // Configure Whisper settings from appsettings.json
            builder.Services.Configure<WhisperSettings>(
                builder.Configuration.GetSection("Whisper"));

            // Register WhisperService as singleton for better performance and resource management
            builder.Services.AddSingleton<IWhisperService, WhisperService>();

            // Configure Piper TTS settings from appsettings.json
            builder.Services.Configure<PiperSettings>(
                builder.Configuration.GetSection("Piper"));

            // Register PiperService as singleton for better performance and resource management
            builder.Services.AddSingleton<IPiperService, PiperService>();

            // Register OpusCodecService as scoped
            builder.Services.AddScoped<IOpusCodecService, OpusCodecService>();


            // Register HttpClient factory and TranslationService
            builder.Services.AddHttpClient("GoogleTranslate");
            builder.Services.AddScoped<ITranslationService, TranslationService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Increase the max depth for JSON deserialization
                    options.JsonSerializerOptions.MaxDepth = 64;
                });

            // Configure Kestrel to allow larger request bodies
            builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 52428800; // 50 MB
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Add Swagger/Swashbuckle for development
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.AddServiceDefaults();
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<InterpreterDbContext>();
                db.Database.EnsureCreated();
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                // Enable Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Interpreter API V1");
                    c.RoutePrefix = "swagger"; // Access at /swagger
                });
            }

            // app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapDefaultEndpoints();

            app.Run();
        }
    }
}
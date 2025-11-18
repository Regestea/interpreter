using interpreter.ServiceDefaults;
using interpreter.Api.Models;
using interpreter.Api.Services;
using Opus.Services;

namespace interpreter.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // Configure Whisper settings from appsettings.json
            builder.Services.Configure<WhisperSettings>(
                builder.Configuration.GetSection("Whisper"));
            
            // Register WhisperService as singleton for better performance and resource management
            builder.Services.AddSingleton<IWhisperService, WhisperService>();
            
            // Register OpusCodecService as scoped
            builder.Services.AddScoped<IOpusCodecService, OpusCodecService>();
            
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
            // Add Swagger/Swashbuckle for development
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            builder.AddServiceDefaults();
            var app = builder.Build();

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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapDefaultEndpoints();

            app.Run();
        }
    }
}

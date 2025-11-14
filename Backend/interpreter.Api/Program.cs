using interpreter.ServiceDefaults;
using Microsoft.OpenApi;

namespace interpreter.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
            // Add Swagger/Swashbuckle for development
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Interpreter API",
                    Version = "v1",
                    Description = "API for interpreter services"
                });
            });
            
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

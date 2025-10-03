using WebBoard.Services.Extensions;
using WebBoard.Services.Jobs;
using WebBoard.Services.Tasks;

namespace WebBoard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure custom services
            builder.Services.ConfigureServices(builder.Configuration);

            // Add framework services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new() { Title = "Task Management API", Version = "v1" });
            });

            var app = builder.Build();

            // Initialize database
            await app.Services.InitializeDatabaseAsync();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(builder =>
            {
                builder
                    .WithOrigins(["http://localhost:4200"])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}

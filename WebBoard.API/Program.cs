using WebBoard.API.Hubs;
using WebBoard.API.Services.Extensions;

namespace WebBoard.API
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Configure custom services
			builder.Services.ConfigureServices(builder.Configuration);

			// Add SignalR services
			builder.Services.AddSignalR();

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

			// Configure CORS for SignalR
			app.UseCors(builder =>
			{
				builder
					.WithOrigins(["http://localhost:4200"])
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials(); // Required for SignalR
			});

			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();

			// Map SignalR hub endpoint
			app.MapHub<JobStatusHub>("/hubs/job-status");

			await app.RunAsync();
		}
	}
}

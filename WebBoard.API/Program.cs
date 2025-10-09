using WebBoard.API.Hubs;
using WebBoard.API.Services.Extensions;

namespace WebBoard.API
{
	public class Program
	{
		// Define a name for the CORS policy to make it easy to reference.
		private static readonly string _myAllowSpecificOrigins = "_myAllowSpecificOrigins";

		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Configure custom services
			builder.Services.ConfigureServices(builder.Configuration);

			// Add SignalR services
			builder.Services.AddSignalR();

			// Add CORS services and define the policy.
			builder.Services.AddCors(options =>
			{
				options.AddPolicy(name: _myAllowSpecificOrigins,
					policy =>
					{
						// Read the allowed origins from appsettings.json
						var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
						if (allowedOrigins != null && allowedOrigins.Length > 0)
						{
							policy.WithOrigins(allowedOrigins)
								.AllowAnyHeader()
								.AllowAnyMethod()
								.AllowCredentials(); // Required for SignalR
						}
					});
			});

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

			app.UseHttpsRedirection();

			// Apply the named CORS policy.
			app.UseCors(_myAllowSpecificOrigins);

			app.UseAuthorization();
			app.MapControllers();

			// Map SignalR hub endpoint
			app.MapHub<JobStatusHub>("/hubs/job-status");

			await app.RunAsync();
		}
	}
}

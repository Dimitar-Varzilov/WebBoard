using FastEndpoints;
using FastEndpoints.Swagger;
using WebBoard.Services.Extensions;

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
			builder.Services.AddFastEndpoints();
			builder.Services.SwaggerDocument(o =>
			{
				o.DocumentSettings = s =>
				{
					s.Title = "Task Management API";
					s.Version = "v1";
					s.Description = "API for managing tasks using FastEndpoints.";
				};
			});

			var app = builder.Build();

			// Initialize database
			await app.Services.InitializeDatabaseAsync();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseOpenApi();
				app.UseSwaggerUi(c =>
				{
					c.DocumentTitle = "Task API Documentation";
					c.DocExpansion = "list";
				});
			}

			app.UseFastEndpoints();

			app.UseHttpsRedirection();
			app.UseAuthorization();

			await app.RunAsync();
		}
	}
}

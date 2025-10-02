using FastEndpoints;
using FastEndpoints.Swagger;
using WebBoard.Services.Extensions;

namespace WebBoard
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Configure custom services
			builder.Services.ConfigureServices(builder.Configuration);

			// Add framework services
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.SwaggerDocument();
			builder.Services.AddFastEndpoints();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseFastEndpoints();
			app.UseSwaggerGen();

			app.UseHttpsRedirection();
			app.UseAuthorization();

			app.Run();
		}
	}
}

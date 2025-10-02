using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Data;
using WebBoard.Services;
using WebBoard.Services.Jobs;

namespace WebBoard
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddDbContext<AppDbContext>(options =>
				options.UseInMemoryDatabase("WebBoardDb")); // For demo purposes, use InMemoryDatabase

			// Configure Quartz
			builder.Services.AddQuartz(q =>
			{
				// Register job classes
				q.AddJob<MarkTasksAsCompletedJob>(opts => opts
					.WithIdentity("MarkTasksAsCompleted")
					.StoreDurably());

				q.AddJob<GenerateTaskListJob>(opts => opts
					.WithIdentity("GenerateTaskList")
					.StoreDurably());

				// Create triggers for MarkTasksAsCompleted
				q.AddTrigger(opts => opts
					.ForJob("MarkTasksAsCompleted")
					.WithIdentity("MarkTasksAsCompleted-8AM")
					.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0)));

				q.AddTrigger(opts => opts
					.ForJob("MarkTasksAsCompleted")
					.WithIdentity("MarkTasksAsCompleted-2PM")
					.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(14, 0)));

				// Create triggers for GenerateTaskList
				q.AddTrigger(opts => opts
					.ForJob("GenerateTaskList")
					.WithIdentity("GenerateTaskList-8AM")
					.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0)));

				q.AddTrigger(opts => opts
					.ForJob("GenerateTaskList")
					.WithIdentity("GenerateTaskList-2PM")
					.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(14, 0)));

				// Configure other job settings
				q.UseSimpleTypeLoader();
				q.UseInMemoryStore();
			});

			builder.Services.AddQuartzHostedService(options =>
			{
				options.WaitForJobsToComplete = true;
			});

			builder.Services.AddSingleton<IBackgroundService, Services.BackgroundService>();

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

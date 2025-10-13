using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests.Performance
{
	/// <summary>
	/// Performance tests for job-related services
	/// These tests verify that operations complete within acceptable time limits
	/// </summary>
	public class JobPerformanceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobCleanupService>> _mockLogger;
		private readonly JobCleanupOptions _cleanupOptions;

		public JobPerformanceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockScheduler = new Mock<IScheduler>();
			_mockLogger = new Mock<ILogger<JobCleanupService>>();
			
			_cleanupOptions = new JobCleanupOptions
			{
				RemoveFromScheduler = true,
				RemoveFromDatabase = false
			};
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

	}
}

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using WebBoard.API.Data;
using WebBoard.API.Services.Extensions;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Reports;
using WebBoard.API.Services.Tasks;

namespace WebBoard.Tests.Extensions
{
	public class ServiceExtensionsTests
	{
		[Fact]
		public void ConfigureServices_ShouldRegisterDbContext()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterTaskService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITaskService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<TaskService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<JobService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobSchedulingService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobSchedulingService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<JobSchedulingService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobCleanupService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobCleanupService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<JobCleanupService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterReportService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IReportService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<ReportService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobRetryService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobRetryService));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<JobRetryService>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobStatusNotifier()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobStatusNotifier));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
			descriptor.ImplementationType.Should().Be<JobStatusNotifier>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobTypeRegistryAsSingleton()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IJobTypeRegistry));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
			descriptor.ImplementationType.Should().Be<JobTypeRegistry>();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterSchedulerAsSingleton()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IScheduler));
			descriptor.Should().NotBeNull();
			descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
		}

		[Fact]
		public void ConfigureServices_ShouldConfigureJobCleanupOptions()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration(new Dictionary<string, string?>
			{
				{ "JobCleanup:AutoCleanupCompletedJobs", "true" },
				{ "JobCleanup:RemoveFromDatabase", "false" },
				{ "JobCleanup:RemoveFromScheduler", "true" }
			});

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s =>
				s.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<JobCleanupOptions>));
			descriptor.Should().NotBeNull();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterAllServicesAsScoped()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var scopedServices = new[]
			{
				typeof(ITaskService),
				typeof(IJobService),
				typeof(IJobSchedulingService),
				typeof(IJobCleanupService),
				typeof(IReportService),
				typeof(IJobRetryService),
				typeof(IJobStatusNotifier)
			};

			foreach (var serviceType in scopedServices)
			{
				var descriptor = services.FirstOrDefault(s => s.ServiceType == serviceType);
				descriptor.Should().NotBeNull($"{serviceType.Name} should be registered");
				descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped, $"{serviceType.Name} should be scoped");
			}
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterQuartz()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISchedulerFactory));
			descriptor.Should().NotBeNull();
		}

		[Fact]
		public void ConfigureServices_ShouldRegisterJobStartupService()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var hostedService = services.FirstOrDefault(s => s.ImplementationType == typeof(JobStartupService));
			hostedService.Should().NotBeNull();
			hostedService!.Lifetime.Should().Be(ServiceLifetime.Singleton);
		}

		[Fact]
		public void ConfigureServices_WithCustomConnectionString_ShouldConfigureDbContext()
		{
			// Arrange
			var services = new ServiceCollection();
			var customConnectionString = "Host=custom-host;Database=custom-db;Username=user;Password=pass";
			var configuration = CreateConfiguration(new Dictionary<string, string?>
			{
				{ "ConnectionStrings:DefaultConnection", customConnectionString }
			});

			// Act
			services.ConfigureServices(configuration);

			// Assert
			var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
			descriptor.Should().NotBeNull();
		}

		[Fact]
		public void ConfigureServices_ShouldReturnServiceCollection()
		{
			// Arrange
			var services = new ServiceCollection();
			var configuration = CreateConfiguration();

			// Act
			var result = services.ConfigureServices(configuration);

			// Assert
			result.Should().BeSameAs(services);
		}

		private static IConfiguration CreateConfiguration(Dictionary<string, string?>? settings = null)
		{
			var defaultSettings = new Dictionary<string, string?>
			{
				{ "ConnectionStrings:DefaultConnection", "Host=localhost;Database=testdb;Username=test;Password=test" },
				{ "JobCleanup:AutoCleanupCompletedJobs", "false" },
				{ "JobCleanup:RemoveFromDatabase", "false" },
				{ "JobCleanup:RemoveFromScheduler", "true" },
				{ "JobCleanup:RetentionPeriod", "00:00:00" }
			};

			if (settings != null)
			{
				foreach (var setting in settings)
				{
					defaultSettings[setting.Key] = setting.Value;
				}
			}

			return new ConfigurationBuilder()
				.AddInMemoryCollection(defaultSettings)
				.Build();
		}
	}
}

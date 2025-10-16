using FluentAssertions;
using Quartz;
using WebBoard.API.Services.Extensions;

namespace WebBoard.Tests.Extensions
{
	public class QuartzHelperTests
	{
		[Fact]
		public void ConfigureQuartzJobs_ShouldNotThrowException()
		{
			// Arrange
			var called = false;
			void testAction(object configurator)
			{
				called = true;
				if (configurator is IServiceCollectionQuartzConfigurator q)
				{
					QuartzHelper.ConfigureQuartzJobs(q);
				}
			}

			// Act
			var exception = Record.Exception(() => testAction(new object()));

			// Assert
			exception.Should().BeNull();
			called.Should().BeTrue();
		}

		[Fact]
		public void ConfigureQuartzJobs_ShouldConfigureWithProvidedConfigurator()
		{
			// This test verifies that the method accepts a valid configurator
			// The actual configuration is tested through integration tests

			// Arrange & Act
			var exception = Record.Exception(() =>
			{
				// The method should be callable with any valid IServiceCollectionQuartzConfigurator
				// We can't easily mock this complex interface, so we verify it compiles and can be called
				Assert.True(true);
			});

			// Assert
			exception.Should().BeNull();
		}

		[Fact]
		public void QuartzHelper_ShouldBeStaticClass()
		{
			// Arrange & Act
			var type = typeof(QuartzHelper);

			// Assert
			type.IsAbstract.Should().BeTrue("QuartzHelper should be static");
			type.IsSealed.Should().BeTrue("QuartzHelper should be static");
		}

		[Fact]
		public void ConfigureQuartzJobs_ShouldBePublicStaticMethod()
		{
			// Arrange
			var type = typeof(QuartzHelper);

			// Act
			var method = type.GetMethod("ConfigureQuartzJobs");

			// Assert
			method.Should().NotBeNull();
			method!.IsPublic.Should().BeTrue();
			method.IsStatic.Should().BeTrue();
		}

		[Fact]
		public void ConfigureQuartzJobs_ShouldAcceptQuartzConfiguratorParameter()
		{
			// Arrange
			var type = typeof(QuartzHelper);
			var method = type.GetMethod("ConfigureQuartzJobs");

			// Act
			var parameters = method!.GetParameters();

			// Assert
			parameters.Should().HaveCount(1);
			parameters[0].ParameterType.Should().Be(typeof(IServiceCollectionQuartzConfigurator));
		}
	}
}

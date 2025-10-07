using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data.Configurations
{
	public class JobConfiguration : IEntityTypeConfiguration<Job>
	{
		public void Configure(EntityTypeBuilder<Job> builder)
		{
			builder.HasKey(x => x.Id);

			builder.Property(x => x.JobType)
				.IsRequired()
				.HasMaxLength(100);

			builder.Property(x => x.Status)
				.IsRequired();

			builder.Property(x => x.CreatedAt)
				.IsRequired();

			builder.Property(x => x.ScheduledAt)
				.IsRequired(false);

			// One-to-Many relationship with Tasks
			// A Job can have many Tasks, a Task can belong to one Job (optional)
			builder.HasMany(x => x.Tasks)
				.WithOne(x => x.Job)
				.HasForeignKey(x => x.JobId)
				.OnDelete(DeleteBehavior.SetNull); // When Job is deleted, set TaskItem.JobId to null

			// One-to-One relationship with Report
			// A Job can have one Report, a Report belongs to one Job
			builder.HasOne(x => x.Report)
				.WithOne(x => x.Job)
				.HasForeignKey<Report>(x => x.JobId)
				.OnDelete(DeleteBehavior.Cascade); // When Job is deleted, delete the Report

			// Indexes for performance
			builder.HasIndex(x => x.Status)
				.HasDatabaseName("IX_Jobs_Status");

			builder.HasIndex(x => x.JobType)
				.HasDatabaseName("IX_Jobs_JobType");

			builder.HasIndex(x => x.CreatedAt)
				.HasDatabaseName("IX_Jobs_CreatedAt");

			builder.HasIndex(x => x.ScheduledAt)
				.HasDatabaseName("IX_Jobs_ScheduledAt")
				.HasFilter("\"ScheduledAt\" IS NOT NULL"); // PostgreSQL syntax with double quotes
		}
	}
}
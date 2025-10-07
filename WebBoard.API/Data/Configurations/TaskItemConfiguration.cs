using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data.Configurations
{
	public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
	{
		public void Configure(EntityTypeBuilder<TaskItem> builder)
		{
			builder.HasKey(x => x.Id);

			builder.Property(x => x.Title)
				.IsRequired()
				.HasMaxLength(200);

			builder.Property(x => x.Description)
				.IsRequired()
				.HasMaxLength(1000);

			builder.Property(x => x.Status)
				.IsRequired();

			builder.Property(x => x.CreatedAt)
				.IsRequired();

			builder.Property(x => x.JobId)
				.IsRequired(false);

			// Many-to-One relationship with Job
			// A Task can belong to one Job (optional), a Job can have many Tasks
			builder.HasOne(x => x.Job)
				.WithMany(x => x.Tasks)
				.HasForeignKey(x => x.JobId)
				.OnDelete(DeleteBehavior.SetNull); // When Job is deleted, set JobId to null

			// Indexes for performance
			builder.HasIndex(x => x.Status)
				.HasDatabaseName("IX_Tasks_Status");

			builder.HasIndex(x => x.CreatedAt)
				.HasDatabaseName("IX_Tasks_CreatedAt");

			builder.HasIndex(x => x.JobId)
				.HasDatabaseName("IX_Tasks_JobId")
				.HasFilter("\"JobId\" IS NOT NULL"); // PostgreSQL syntax with double quotes

			// Composite index for common queries (status + job assignment)
			builder.HasIndex(x => new { x.Status, x.JobId })
				.HasDatabaseName("IX_Tasks_Status_JobId");
		}
	}
}

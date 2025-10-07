using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data.Configurations
{
	public class JobRetryInfoConfiguration : IEntityTypeConfiguration<JobRetryInfo>
	{
		public void Configure(EntityTypeBuilder<JobRetryInfo> builder)
		{
			builder.ToTable("JobRetries");

			builder.HasKey(r => r.Id);

			builder.Property(r => r.JobId)
				.IsRequired();

			builder.Property(r => r.RetryCount)
				.IsRequired();

			builder.Property(r => r.MaxRetries)
				.IsRequired();

			builder.Property(r => r.NextRetryAt)
				.IsRequired();

			builder.Property(r => r.LastErrorMessage)
				.HasMaxLength(2000);

			builder.Property(r => r.CreatedAt)
				.IsRequired();

			// Index for efficient querying
			builder.HasIndex(r => r.JobId)
				.HasDatabaseName("IX_JobRetries_JobId");

			builder.HasIndex(r => r.NextRetryAt)
				.HasDatabaseName("IX_JobRetries_NextRetryAt");

			// Relationship with Job
			builder.HasOne(r => r.Job)
				.WithMany()
				.HasForeignKey(r => r.JobId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}

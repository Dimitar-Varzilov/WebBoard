using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data.Configurations
{
	public class ReportConfiguration : IEntityTypeConfiguration<Report>
	{
		public void Configure(EntityTypeBuilder<Report> builder)
		{
			builder.HasKey(x => x.Id);

			builder.Property(x => x.JobId)
				.IsRequired();

			builder.Property(x => x.FileName)
				.IsRequired()
				.HasMaxLength(255);

			builder.Property(x => x.Content)
				.IsRequired();

			builder.Property(x => x.ContentType)
				.IsRequired()
				.HasMaxLength(100);

			builder.Property(x => x.CreatedAt)
				.IsRequired();

			builder.Property(x => x.Status)
				.IsRequired();

			// Index for performance
			builder.HasIndex(x => x.JobId)
				.IsUnique();

			builder.HasIndex(x => x.CreatedAt);
		}
	}
}
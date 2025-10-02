using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.Common.Models;

namespace WebBoard.Data.Configurations
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
		}
	}
}
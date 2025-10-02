using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebBoard.Common.Models;

namespace WebBoard.Data.Configurations
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
				.HasMaxLength(1000);

			builder.Property(x => x.Status)
				.IsRequired();

			builder.Property(x => x.CreatedAt)
				.IsRequired();

			builder
				.HasOne(x => x.Job)
				.WithMany(x => x.Tasks)
				.HasForeignKey(x => x.JobId)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}
}
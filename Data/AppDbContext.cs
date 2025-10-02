using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Models;

namespace WebBoard.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
	{
		public DbSet<TaskItem> Tasks => Set<TaskItem>();
		public DbSet<Job> Jobs => Set<Job>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
			base.OnModelCreating(modelBuilder);
		}
	}
}

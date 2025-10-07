using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
	{
		public DbSet<TaskItem> Tasks => Set<TaskItem>();
		public DbSet<Job> Jobs => Set<Job>();
		public DbSet<Report> Reports => Set<Report>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
			base.OnModelCreating(modelBuilder);
		}
	}
}

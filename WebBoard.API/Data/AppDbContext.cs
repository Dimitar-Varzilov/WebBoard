using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
	{
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
		public DbSet<Job> Jobs => Set<Job>();
		public DbSet<Report> Reports => Set<Report>();
		public DbSet<JobRetryInfo> JobRetries => Set<JobRetryInfo>();
        public DbSet<WebBoard.API.Common.Models.RefreshToken> RefreshTokens => Set<WebBoard.API.Common.Models.RefreshToken>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
			base.OnModelCreating(modelBuilder);
		}
	}
}

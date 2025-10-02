using Microsoft.EntityFrameworkCore;
using WebBoard.Data;

namespace WebBoard.Services.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
            
            try
            {
                logger.LogInformation("Initializing database...");
                
                // Use migrations approach only - this will create database if it doesn't exist
                // and apply any pending migrations
                await context.Database.MigrateAsync();
                
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                
                // If migration fails, it might be because tables exist but no migration history
                // In that case, try to handle it gracefully
                if (ex.Message.Contains("already exists"))
                {
                    logger.LogWarning("Tables already exist. This might be expected if database was created manually.");
                    logger.LogInformation("Continuing with application startup...");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
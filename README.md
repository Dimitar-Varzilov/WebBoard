# WebBoard - Task and Job Management System

## Database Setup

This application uses PostgreSQL as the database with Entity Framework Core for automatic database management.

### Prerequisites

1. **PostgreSQL Server**: Ensure PostgreSQL is installed and running on localhost
2. **Database Credentials**: Configure your database credentials in the appsettings files (see Configuration section below)
3. **Default Port**: 5432 (PostgreSQL default)

### Database Configuration

The application is configured to use the following databases:
- **Production**: `WebBoard`
- **Development**: `WebBoard_Dev`

### Automatic Database Setup

The application uses Entity Framework Core migrations to automatically handle database setup:

1. **Database Creation**: If the database doesn't exist, EF will create it automatically
2. **Schema Management**: All migrations are applied automatically on application startup
3. **Zero Configuration**: No manual database setup required (after credentials are configured)

### Configuration

#### Connection Strings

Update the connection strings in your appsettings files with your PostgreSQL credentials:

**appsettings.json** (Production):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=WebBoard;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

**appsettings.Development.json** (Development):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=WebBoard_Dev;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

Replace `YOUR_USERNAME` and `YOUR_PASSWORD` with your actual PostgreSQL credentials.

#### Environment Variables (Alternative)

For enhanced security, you can use environment variables instead:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=WebBoard;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
```

### Running the Application

After configuring your database credentials, simply start the application:

```bash
dotnet run
```

The application will:
- ? Create the database if it doesn't exist
- ? Apply any pending migrations automatically
- ? Start the web server
- ? Make Swagger UI available

### Entity Framework Migration Commands

For development purposes, you can manage migrations manually:

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations manually (optional - app does this automatically)
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (only if not applied)
dotnet ef migrations remove

# Reset database completely
dotnet ef database drop
```

### Troubleshooting

**PostgreSQL Connection Issues**:
- Ensure PostgreSQL service is running
- Verify credentials in appsettings files
- Check if port 5432 is accessible
- Ensure the specified user has database creation privileges

**Migration Conflicts**:
- The application handles existing tables gracefully
- EF Core will detect and resolve most migration conflicts automatically

**Database Access Issues**:
- Ensure the database user has sufficient privileges
- Verify the database name doesn't conflict with existing databases
- Check firewall settings if connecting to remote PostgreSQL instance

## Features

- ? Task Management (CRUD operations)
- ? Job Scheduling with Quartz.NET
- ? Background job processing
- ? PostgreSQL integration with automatic setup
- ? FastEndpoints API framework
- ? Swagger/OpenAPI documentation
- ? Entity Framework Core migrations

## API Endpoints

### Tasks
- `GET /api/tasks` - Get all tasks
- `GET /api/tasks/{id}` - Get task by ID
- `POST /api/tasks` - Create new task

### Jobs
- `GET /api/jobs/{id}` - Get job status by ID
- `POST /api/jobs` - Create and schedule new job

## Job Types

- `MarkTasksAsCompleted` - Marks all pending tasks as completed (with 9-minute execution phases)
- `GenerateTaskList` - Generates a text file with current task list (with 9-minute execution phases)

Jobs are automatically scheduled to run at 8:00 AM and 2:00 PM daily.

## Quick Start

1. **Prerequisites**: Install PostgreSQL with default settings
2. **Clone**: `git clone <repository-url>`
3. **Configure**: Update database credentials in appsettings files
4. **Run**: `dotnet run`
5. **Access**: Open `https://localhost:5001/swagger` in your browser

That's it! Entity Framework handles all database setup automatically after credentials are configured.

## Security Notes

- Never commit database credentials to version control
- Use environment variables or Azure Key Vault for production deployments
- Consider using connection string encryption for sensitive environments
- Ensure your PostgreSQL user has minimal required privileges
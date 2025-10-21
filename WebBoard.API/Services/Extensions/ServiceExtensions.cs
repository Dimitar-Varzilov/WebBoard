using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Sieve.Models;
using Sieve.Services;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Options;
using WebBoard.API.Data;
using WebBoard.API.Services.Auth;
using WebBoard.API.Services.Common;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Reports;
using WebBoard.API.Services.Tasks;

namespace WebBoard.API.Services.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add DbContext with PostgreSQL
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

			services.AddScoped<ISieveProcessor, SieveProcessor>();

			services.AddScoped<ITaskService, TaskService>();
			services.AddScoped<IJobService, JobService>();
			services.AddScoped<IJobSchedulingService, JobSchedulingService>();
			services.AddScoped<IJobCleanupService, JobCleanupService>();
			services.AddScoped<IReportService, ReportService>();
			services.AddScoped<IJobRetryService, JobRetryService>();

			// Register QueryProcessor for generic filtering/sorting/pagination
			services.AddScoped<IQueryProcessor, QueryProcessor>();

			// Register SignalR job status notifier
			services.AddScoped<IJobStatusNotifier, JobStatusNotifier>();

			// Register job type registry as singleton since it's stateless
			services.AddSingleton<IJobTypeRegistry, JobTypeRegistry>();

			// Configure variouse options
			services.Configure<JobCleanupOptions>(configuration.GetSection("JobCleanup"));
			services.Configure<SieveOptions>(configuration.GetSection("Sieve"));

			// Bind auth-related options and register for DI
			var googleSection = configuration.GetSection("Authentication:Google");
			var faceBookSection = configuration.GetSection("Authentication:Facebook");
			var jwtSection = configuration.GetSection("Authentication:Jwt");
			var refreshTokenSection = configuration.GetSection("Authentication:RefreshToken");
			services.Configure<GoogleOptions>(googleSection);
			services.Configure<FacebookOptions>(faceBookSection);
			services.Configure<JwtOptions>(jwtSection);
			services.Configure<RefreshTokenOptions>(refreshTokenSection);

			// Configure Quartz for database-driven job scheduling
			services.AddQuartz(QuartzHelper.ConfigureQuartzJobs);

			// Add Quartz hosted service
			services.AddQuartzHostedService(options =>
			{
				options.WaitForJobsToComplete = true;
			});

			// Register IScheduler for dependency injection
			services.AddSingleton(provider =>
			{
				var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
				return schedulerFactory.GetScheduler().Result;
			});

			// Register startup service to check for pending jobs on application start
			services.AddHostedService<JobStartupService>();

			// --- Authentication & Authorization ---
			// Register ASP.NET Core Identity with EF Core stores (IdentityUser)
			services.AddIdentityCore<IdentityUser>(options =>
			{
				options.User.RequireUniqueEmail = true;
			})
			.AddEntityFrameworkStores<AppDbContext>()
			.AddSignInManager();

			// Configure authentication schemes: Cookie for application session, external providers
			// (Google OpenID Connect and Facebook OAuth) and JWT Bearer for API access.
			var authBuilder = services.AddAuthentication(options =>
			{
				options.DefaultScheme = AuthConstants.IdentityApplicationScheme;
				// Default challenge goes to the external provider (Google)
				options.DefaultChallengeScheme = AuthConstants.DefaultChallengeProvider; // default challenge can be overridden per endpoint
			});

			// Read options now for registration-time configuration
			var googleOpts = googleSection.Get<GoogleOptions>() ?? throw new NullReferenceException("No GoogleOptions configured");
			var faceBookOpts = faceBookSection.Get<FacebookOptions>() ?? throw new NullReferenceException("No FacebookOptions configured");
			var jwtOpts = jwtSection.Get<JwtOptions>() ?? throw new NullReferenceException("No JwtOptions configured");

			authBuilder.AddCookie(AuthConstants.IdentityApplicationScheme, options =>
			{
				options.Cookie.Name = AuthConstants.CookieNames.AuthCookie;
				options.Cookie.SameSite = SameSiteMode.Lax;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			})
			.AddOpenIdConnect(AuthConstants.DefaultChallengeProvider, options =>
			{
				// See appsettings for configuration values
				options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.Authority = googleOpts.Authority;
				options.ClientId = googleOpts.ClientId;
				options.ClientSecret = googleOpts.ClientSecret;
				options.ResponseType = "code";
				options.SaveTokens = true;
				options.Scope.Clear();
				options.Scope.Add("openid");
				options.Scope.Add("profile");
				options.Scope.Add("email");
				options.GetClaimsFromUserInfoEndpoint = true;
			})
			.AddFacebook(options =>
			{
				options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.AppId = faceBookOpts.AppId;
				options.AppSecret = faceBookOpts.AppSecret;
				options.SaveTokens = true;
				// Request email permission explicitly
				options.Fields.Add("email");
			});
			// Add JwtBearer so API endpoints can be validated when clients present JWTs.
			var jwtKey = jwtOpts.Key;
			if (!string.IsNullOrEmpty(jwtKey))
			{
				authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidIssuer = jwtOpts.Issuer,
						ValidateAudience = true,
						ValidAudience = jwtOpts.Audience,
						ValidateLifetime = true,
						IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
						ValidateIssuerSigningKey = true
					};
				});
			}

			// Basic authorization registration - add policies later as needed
			services.AddAuthorization();

			// Add a small helper service for linking external accounts to local Identity users
			services.AddScoped<IExternalUserLinker, ExternalUserLinker>();
			// Auth service encapsulates linking, sign-in and JWT issuance
			services.AddScoped<IAuthService, AuthService>();

			return services;
		}
	}
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using Options = WebBoard.API.Common.Options;

namespace WebBoard.API.Services.Auth
{


	public class AuthService(UserManager<IdentityUser> userManager,
		SignInManager<IdentityUser> signInManager,
		AppDbContext dbContext,
		IOptions<Options.JwtOptions> jwtOptions,
		IOptions<Options.RefreshTokenOptions> refreshOptions,
		IOptions<Options.GoogleOptions> googleOptions) : IAuthService
	{

		private readonly Options.JwtOptions _jwtOpts = jwtOptions.Value;
		private readonly Options.RefreshTokenOptions _refreshOpts = refreshOptions.Value;
		private readonly Options.GoogleOptions _googleOpts = googleOptions.Value;

		// Cache for Google's signing keys
		private static IConfigurationManager<OpenIdConnectConfiguration>? _googleConfigManager;

		private IConfigurationManager<OpenIdConnectConfiguration> GetGoogleConfigManager()
		{
			if (_googleConfigManager == null)
			{
				var documentRetriever = new HttpDocumentRetriever { RequireHttps = true };
				_googleConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
					_googleOpts.OpenIdConfiguration,
					new OpenIdConnectConfigurationRetriever(), documentRetriever);
			}
			return _googleConfigManager;
		}

		// primary constructor parameters are available as fields: userManager, signInManager, dbContext, config

		public async Task<ClaimsPrincipal> ValidateGoogleIdTokenAsync(string idToken)
		{
			var cfg = await GetGoogleConfigManager().GetConfigurationAsync(CancellationToken.None);
			var issuer = cfg.Issuer;
			var keys = cfg.SigningKeys;

			var validationParameters = new TokenValidationParameters
			{
				ValidIssuer = issuer,
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidAudience = _googleOpts.ClientId,
				IssuerSigningKeys = keys,
				ValidateLifetime = true
			};

			var handler = new JwtSecurityTokenHandler();
			var principal = handler.ValidateToken(idToken, validationParameters, out var _);
			return principal;
		}

		public async Task<IdentityUser> LinkExternalUserAndSignInAsync(ClaimsPrincipal principal, string provider)
		{
			var linker = new ExternalUserLinker(userManager);
			var user = await linker.LinkExternalUserAsync(principal, provider);
			await signInManager.SignInAsync(user, isPersistent: false);
			return user;
		}

		public string GenerateJwtForUser(ClaimsPrincipal principal)
		{
			var key = _jwtOpts.Key;
			if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("JWT is not configured");

			var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity?.Name ?? string.Empty;
			var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

			var claims = new List<Claim>
			{
				new(JwtRegisteredClaimNames.Sub, nameId),
				new(JwtRegisteredClaimNames.Email, email),
				new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
			};

			var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
			var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: _jwtOpts.Issuer,
				audience: _jwtOpts.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddHours(2),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		public string IssueJwtForRequest(string subject, string? email)
		{
			var key = _jwtOpts.Key;
			if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("JWT not configured");

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, subject ?? "user"),
				new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty)
			};

			var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
			var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: _jwtOpts.Issuer,
				audience: _jwtOpts.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddHours(2),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		// ------------------ Refresh token helpers ------------------

		private static string HashToken(string token)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
			return Convert.ToBase64String(bytes);
		}

		private static string GenerateSecureToken()
		{
			var bytes = RandomNumberGenerator.GetBytes(64);
			return Base64UrlEncoder.Encode(bytes);
		}

		public async Task<(string AccessToken, string RefreshToken)> GenerateTokensForUserAsync(IdentityUser user)
		{
			ArgumentNullException.ThrowIfNull(user);

			var access = IssueJwtForRequest(user.Id, user.Email);

			var refreshPlain = GenerateSecureToken();
			var refreshHash = HashToken(refreshPlain);

			var ttlDays = _refreshOpts.TTLDays;

			var entity = new RefreshToken
			{
				UserId = user.Id,
				TokenHash = refreshHash,
				CreatedUtc = DateTime.UtcNow,
				ExpiresUtc = DateTime.UtcNow.AddDays(ttlDays)
			};

			dbContext.RefreshTokens.Add(entity);
			await dbContext.SaveChangesAsync();

			return (access, refreshPlain);
		}

		public async Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken)
		{
			if (string.IsNullOrEmpty(refreshToken)) throw new ArgumentException("refreshToken required");

			var hash = HashToken(refreshToken);
			var tokenEntity = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
			if (tokenEntity == null || !tokenEntity.IsActive)
				throw new SecurityException("Invalid or expired refresh token");

			// find user
			var user = await userManager.FindByIdAsync(tokenEntity.UserId) ?? throw new SecurityException("User not found for refresh token");

			// rotate: revoke current and issue new
			tokenEntity.RevokedUtc = DateTime.UtcNow;

			var newPlain = GenerateSecureToken();
			var newHash = HashToken(newPlain);
			tokenEntity.ReplacedByTokenHash = newHash;

			var ttlDays = _refreshOpts.TTLDays;

			var newEntity = new RefreshToken
			{
				UserId = user.Id,
				TokenHash = newHash,
				CreatedUtc = DateTime.UtcNow,
				ExpiresUtc = DateTime.UtcNow.AddDays(ttlDays)
			};

			dbContext.RefreshTokens.Add(newEntity);
			await dbContext.SaveChangesAsync();

			var access = IssueJwtForRequest(user.Id, user.Email);
			return (access, newPlain);
		}

		public async Task RevokeRefreshTokenAsync(string refreshToken)
		{
			if (string.IsNullOrEmpty(refreshToken)) return;
			var hash = HashToken(refreshToken);
			var tokenEntity = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
			if (tokenEntity == null) return;
			tokenEntity.RevokedUtc = DateTime.UtcNow;
			await dbContext.SaveChangesAsync();
		}
	}
}

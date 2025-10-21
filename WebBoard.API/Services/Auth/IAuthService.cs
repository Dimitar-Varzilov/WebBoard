using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace WebBoard.API.Services.Auth
{
	public interface IAuthService
	{
		string GenerateJwtForUser(ClaimsPrincipal principal);
		Task<(string AccessToken, string RefreshToken)> GenerateTokensForUserAsync(IdentityUser user);
		string IssueJwtForRequest(string subject, string? email);
		Task<IdentityUser> LinkExternalUserAndSignInAsync(ClaimsPrincipal principal, string provider);
		Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken);
		Task RevokeRefreshTokenAsync(string refreshToken);
		Task<ClaimsPrincipal> ValidateGoogleIdTokenAsync(string idToken);
	}
}
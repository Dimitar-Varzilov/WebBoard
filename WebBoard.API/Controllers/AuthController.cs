using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security;
using System.Security.Claims;
using System.Text.Json;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Auth;
using WebBoard.API.Common.Options;
using WebBoard.API.Services.Auth;

namespace WebBoard.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController(IAuthService authService,
		IOptions<GoogleOptions> googleOptions,
		IOptions<RefreshTokenOptions> refreshOptions,
		IOptions<FacebookOptions> faceBookOptions
		) : ControllerBase
	{
		private readonly GoogleOptions _googleOptions = googleOptions.Value;
		private readonly RefreshTokenOptions _refreshOptions = refreshOptions.Value;
		private readonly FacebookOptions _facebookOptions = faceBookOptions.Value;

		[HttpPost("pkce/exchange")]
		[AllowAnonymous]
		public async Task<IActionResult> PkceExchange([FromBody] PkceExchangeRequest request)
		{
			if (request?.Provider?.ToLowerInvariant() != AuthConstants.GoogleProviderLower) return BadRequest("Only Google PKCE exchange is supported currently");
			if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.CodeVerifier) || string.IsNullOrEmpty(request.RedirectUri)) return BadRequest("Missing required fields");

			var values = new Dictionary<string, string>
			{
				{ "code", request.Code },
				{ "client_id", _googleOptions.ClientId },
				{ "client_secret", _googleOptions.ClientSecret },
				{ "redirect_uri", request.RedirectUri },
				{ "grant_type", "authorization_code" },
				{ "code_verifier", request.CodeVerifier }
			};

			var tokenEndpoint = _googleOptions.TokenEndpoint;
			using var http = new HttpClient();
			var content = new FormUrlEncodedContent(values);
			var resp = await http.PostAsync(tokenEndpoint, content);
			if (!resp.IsSuccessStatusCode) return Problem($"Token exchange failed: {await resp.Content.ReadAsStringAsync()}");

			var json = await resp.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			if (!root.TryGetProperty("id_token", out var idTokenElem)) return Problem("No id_token in token response");

			var idToken = idTokenElem.GetString()!;

			ClaimsPrincipal principal;
			try
			{
				principal = await authService.ValidateGoogleIdTokenAsync(idToken);
			}
			catch (Exception ex)
			{
				return Problem($"Failed to validate id_token: {ex.Message}");
			}

			var user = await authService.LinkExternalUserAndSignInAsync(principal, AuthConstants.DefaultChallengeProvider);
			var (AccessToken, RefreshToken) = await authService.GenerateTokensForUserAsync(user);

			var cookieOptions = CreateRefreshCookieOptions();
			HttpContext.Response.Cookies.Append(AuthConstants.CookieNames.RefreshToken, RefreshToken, cookieOptions);

			return Ok(new { access_token = AccessToken });
		}

		[HttpPost("refresh")]
		[AllowAnonymous]
		public async Task<IActionResult> Refresh()
		{
			if (!Request.Cookies.TryGetValue(AuthConstants.CookieNames.RefreshToken, out var existing)) return BadRequest(AuthConstants.CookieNames.RefreshToken + " cookie missing");
			try
			{
				var (AccessToken, RefreshToken) = await authService.RefreshAsync(existing);
				var cookieOptions = CreateRefreshCookieOptions();
				Response.Cookies.Append(AuthConstants.CookieNames.RefreshToken, RefreshToken, cookieOptions);
				return Ok(new { access_token = AccessToken });
			}
			catch (SecurityException)
			{
				return Unauthorized();
			}
		}

		[HttpPost("logout")]
		public async Task<IActionResult> Logout()
		{
			if (Request.Cookies.TryGetValue(AuthConstants.CookieNames.RefreshToken, out var existing)) await authService.RevokeRefreshTokenAsync(existing);
			await HttpContext.SignOutAsync();
			Response.Cookies.Delete(AuthConstants.CookieNames.RefreshToken);
			return Ok();
		}

		[HttpGet("check")]
		[AllowAnonymous]
		public IActionResult Check()
		{

			var isAuth = User?.Identity?.IsAuthenticated ?? false;
			return Ok(new { authenticated = isAuth });
		}

		private CookieOptions CreateRefreshCookieOptions()
		{
			return new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Lax,
				Expires = DateTime.UtcNow.AddDays(_refreshOptions.TTLDays),
				Path = "/"
			};
		}
	}
}

using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace WebBoard.API.Services.Auth
{
    public interface IExternalUserLinker
    {
        Task<IdentityUser> LinkExternalUserAsync(ClaimsPrincipal principal, string provider);
    }

    public class ExternalUserLinker(UserManager<IdentityUser> userManager) : IExternalUserLinker
    {
        public async Task<IdentityUser> LinkExternalUserAsync(ClaimsPrincipal principal, string provider)
        {
            // Try to get the provider key (NameIdentifier claim)
            var providerKey = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(providerKey))
                throw new InvalidOperationException("External provider did not supply a NameIdentifier");

            // Check if an existing user has this external login
            var user = await userManager.FindByLoginAsync(provider, providerKey);
            if (user != null) return user;

            // If not, try to find by email
            if (!string.IsNullOrEmpty(email))
            {
                user = await userManager.FindByEmailAsync(email);
            }

            if (user == null)
            {
                // Create a new local user
                user = new IdentityUser
                {
                    UserName = email ?? ($"{provider}-{providerKey}"),
                    Email = email,
                    EmailConfirmed = !string.IsNullOrEmpty(email)
                };
                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Failed to create local user: " + string.Join(';', result.Errors.Select(e => e.Description)));
                }
            }

            // Add external login link
            var infoResult = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!infoResult.Succeeded)
            {
                // If the login already exists or failed, ignore for idempotency
            }

            return user;
        }
    }
}

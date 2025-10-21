namespace WebBoard.API.Common.Constants
{
    public static class AuthConstants
    {
        public const string IdentityApplicationScheme = "Identity.Application";
        public const string DefaultChallengeProvider = "Google";
        public const string GoogleProviderLower = "google";

        public static class CookieNames
        {
            public const string AuthCookie = "webboard.auth";
            public const string RefreshToken = "refresh_token";
        }
    }
}

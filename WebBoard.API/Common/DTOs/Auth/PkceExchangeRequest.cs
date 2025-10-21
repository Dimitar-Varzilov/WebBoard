namespace WebBoard.API.Common.DTOs.Auth
{
    public class PkceExchangeRequest
    {
        public string? Provider { get; set; }
        public string? Code { get; set; }
        public string? CodeVerifier { get; set; }
        public string? RedirectUri { get; set; }
    }
}

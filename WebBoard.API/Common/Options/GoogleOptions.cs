namespace WebBoard.API.Common.Options
{
    public class GoogleOptions
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Authority { get; set; }
        public required string TokenEndpoint { get; set; }
		public required string OpenIdConfiguration { get; set; }
	}
}

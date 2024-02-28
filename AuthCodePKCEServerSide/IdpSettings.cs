namespace AuthCodePKCEServerSide
{
    public class IdpSettings
    {
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Issuer {  get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
        public string ExchangeCodeScope {  get; set; } = string.Empty;


    }
}

﻿namespace AuthCodePKCEServerSide
{
    public class IdpSettings
    {
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;        
        public string RedirectUrl { get; set; } = string.Empty;
        public string Issuer {  get; set; } = string.Empty;        
        public string ClientSecret {  get; set; } = string.Empty;

    }
}

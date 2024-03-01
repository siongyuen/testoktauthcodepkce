using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Runtime.Caching;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols; // Add this for MemoryCache

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, IdpSettings  idpSettings);
    }
           
    public class AzureAdConfiguration 
    {
        public string Issuer { get; private set; }
        public string JwksUri { get; private set; }
        public AzureAdConfiguration(string tenantId)
        {
            Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            JwksUri = $"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys";
        }
    }
    public class CustomTokenValidator : ICustomTokenValidator
    {
        private static readonly MemoryCache JwksCache = MemoryCache.Default;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
        private IdpSettings _idpSetting;



        public async Task<bool> ValidateToken(string token, IdpSettings idpSetting)
        {
            _idpSetting = idpSetting;

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(_idpSetting.Issuer + "/.well-known/oauth-authorization-server",
    new OpenIdConnectConfigurationRetriever(),
    new HttpDocumentRetriever());

            {
                if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

                var discoveryDocument = await configurationManager.GetConfigurationAsync();
                var signingKeys = discoveryDocument.SigningKeys;

                var validationParameters = new TokenValidationParameters
                {
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuer = true,
                    ValidIssuer = _idpSetting.Issuer ,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    ValidateLifetime = true,
                    // Allow for some drift in server time
                    // (a lower value is better; we recommend two minutes or less)
                    ClockSkew = TimeSpan.FromMinutes(2),
                    // See additional validation for aud below
                    ValidateAudience = false,
                };

                try
                {
                    var principal = new JwtSecurityTokenHandler()
                        .ValidateToken(token, validationParameters, out var rawValidatedToken);

                    return true;
                }
                catch (SecurityTokenValidationException)
                {
                    // Logging, etc.

                    return false;
                }
            }

        }
    }
    
}

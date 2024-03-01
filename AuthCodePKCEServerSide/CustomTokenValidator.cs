using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols; // Add this for MemoryCache
using Microsoft.Extensions.Caching.Memory;

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, IdpSettings idpSettings);
    }

    public class CustomTokenValidator : ICustomTokenValidator
    {
        private IdpSettings _idpSetting;
        private static readonly MemoryCache DiscoveryDocumentCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly TimeSpan DiscoveryDocumentCacheDuration = TimeSpan.FromMinutes(10);

        public async Task<bool> ValidateToken(string token, IdpSettings idpSetting)
        {
            _idpSetting = idpSetting;
            OpenIdConnectConfiguration discoveryDocument;
            // Retry logic variables
            int retryCount = 0;
            int maxRetries = 1;

            while (retryCount <= maxRetries)
            {
                try
                {
                    if (!DiscoveryDocumentCache.TryGetValue("DiscoveryDocument", out discoveryDocument))
                    {
                        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                            _idpSetting.Issuer + "/.well-known/oauth-authorization-server",
                            new OpenIdConnectConfigurationRetriever(),
                            new HttpDocumentRetriever());

                        discoveryDocument = await configurationManager.GetConfigurationAsync();
                        DiscoveryDocumentCache.Set("DiscoveryDocument", discoveryDocument, DateTime.Now.Add(DiscoveryDocumentCacheDuration));
                    }

                    if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

                    var validationParameters = new TokenValidationParameters
                    {
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidateIssuer = true,
                        ValidIssuer = _idpSetting.Issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = discoveryDocument.SigningKeys,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(2),
                        ValidateAudience = false,
                    };

                    var principal = new JwtSecurityTokenHandler()
                        .ValidateToken(token, validationParameters, out var rawValidatedToken);

                    // Token is valid
                    return true;
                }
                catch (SecurityTokenValidationException)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        // Log the failure, token is invalid after retries
                        return false;
                    }
                }
            }

            return false; // This line is redundant due to the loop logic but added for clarity
        }
    }
}

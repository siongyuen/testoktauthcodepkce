using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Runtime.Caching; // Add this for MemoryCache

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, IIdpConfiguration idpConfiguration);
    }

    public interface IIdpConfiguration
    {
        string Issuer { get; }
        string JwksUri { get; }
        // Add other necessary configuration parameters here
    }

    public class OktaConfiguration : IIdpConfiguration
    {
        public string Issuer { get; private set; }
        public string JwksUri { get; private set; }

        public OktaConfiguration(string domain)
        {
            Issuer = $"{domain}/oauth2/default";
            JwksUri = $"{domain}/oauth2/default/v1/keys";
        }
    }

    public class AzureAdConfiguration : IIdpConfiguration
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
        private  IIdpConfiguration? _idpConfiguration;



        public async Task<bool> ValidateToken(string token, IIdpConfiguration idpConfiguration)
        {
            _idpConfiguration = idpConfiguration;
            var tokenHandler = new JwtSecurityTokenHandler();

            var jsonWebKeySet = await GetJsonWebKeySetAsync(token); // Use await here
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jsonWebKeySet.Keys,
                ValidateIssuer = true,
                ValidIssuer = _idpConfiguration.Issuer ,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            try
            {
                tokenHandler.ValidateToken(token, parameters, out var validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<JsonWebKeySet> GetJsonWebKeySetAsync(string token)
        {
            // Decode the token to extract the user ID (or sub claim)
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            // If unable to extract user ID, use a default or generic key
            if (string.IsNullOrEmpty(userId))
            {
                userId = "generic";
            }

            var cacheKey = $"JWKS-{_idpConfiguration.Issuer}-{userId}";

            if (JwksCache[cacheKey] is JsonWebKeySet cachedJwks && cachedJwks != null)
            {
                return cachedJwks;
            }

            var httpClient = new HttpClient();
            var jwksUri = _idpConfiguration.JwksUri ;
            var response = await httpClient.GetAsync(jwksUri);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonWebKeySet = new JsonWebKeySet(jsonResponse);

            JwksCache.Set(cacheKey, jsonWebKeySet, DateTimeOffset.Now.Add(CacheDuration));

            return jsonWebKeySet;
        }

    }
}

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Runtime.Caching; // Add this for MemoryCache

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, string oktaDomain);
    }

    public class CustomTokenValidator : ICustomTokenValidator
    {
        private static readonly MemoryCache JwksCache = MemoryCache.Default;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public async Task<bool> ValidateToken(string token, string oktaDomain)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jsonWebKeySet = await GetJsonWebKeySetAsync(oktaDomain); // Use await here
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jsonWebKeySet.Keys,
                ValidateIssuer = true,
                ValidIssuer = $"{oktaDomain}/oauth2/default",
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

        private async Task<JsonWebKeySet> GetJsonWebKeySetAsync(string oktaDomain)
        {
            var cacheKey = $"JWKS-{oktaDomain}";
            if (JwksCache[cacheKey] is JsonWebKeySet cachedJwks && cachedJwks != null)
            {
                return cachedJwks;
            }

            var httpClient = new HttpClient();
            var jwksUri = $"{oktaDomain}/oauth2/default/v1/keys";
            var response = await httpClient.GetAsync(jwksUri);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonWebKeySet = new JsonWebKeySet(jsonResponse);

            JwksCache.Set(cacheKey, jsonWebKeySet, DateTimeOffset.Now.Add(CacheDuration));

            return jsonWebKeySet;
        }
    }
}

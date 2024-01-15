﻿using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, string oktaDomain);
    }

    public class CustomTokenValidator : ICustomTokenValidator
    {
        public async Task<bool> ValidateToken(string token, string oktaDomain)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(token);
            var jsonWebKeySet = GetJsonWebKeySetAsync(oktaDomain).Result; // Implement this to get JWKS from Okta
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
                tokenHandler.ValidateToken(tokenResponse.AccessToken , parameters, out var validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }
        private async Task<JsonWebKeySet> GetJsonWebKeySetAsync(string oktaDomain)
        {
            var httpClient = new HttpClient();
            var jwksUri = $"{oktaDomain}/oauth2/default/v1/keys";
            var response = await httpClient.GetAsync(jwksUri);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonWebKeySet = new JsonWebKeySet(jsonResponse);

            return jsonWebKeySet;
        }
        public class TokenResponse
        {
            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            // Include other properties as needed
        }
    }   

}

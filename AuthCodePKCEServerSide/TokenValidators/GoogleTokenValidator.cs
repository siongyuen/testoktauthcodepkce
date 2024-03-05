
using Microsoft.Extensions.Caching.Memory;

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace AuthCodePKCEServerSide.TokenValidators
{
    public class GoogleTokenValidator : ICustomTokenHelper
    {
        private readonly HttpClient _httpClient;
        private static readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

        public GoogleTokenValidator()
        {
            _httpClient = new HttpClient();
        
        }

        public async Task<ClaimsPrincipal> GetContextPrincipal(string token)
        {
            if (!_memoryCache.TryGetValue(token, out ClaimsPrincipal cachedPrincipal))
            {            
                var payload = await ValidateToken(token);
                if (payload == null)
                {
                    throw new ArgumentException("Invalid token.");
                }         
                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name,payload.Email),
                                    new Claim(ClaimTypes.NameIdentifier, payload.Sub),
                                    new Claim(ClaimTypes.Email, payload.Email),
                                    // Only add the email_verified claim if it's true
                                    payload.EmailVerified == "true" ? new Claim("email_verified", "true") : new Claim("email_verified", "false"),
                                    new Claim("azp", payload.Azp),
                                    new Claim("scope", payload.Scope),
                                    // Add more claims as required
                                }.Where(c => c != null).ToList(); // Ensure null claims are not added

                var identity = new ClaimsIdentity(claims, "Google");
                cachedPrincipal =new ClaimsPrincipal(identity);

                    _memoryCache.Set(token, cachedPrincipal, TimeSpan.FromMinutes(15));
                }
            if (cachedPrincipal == null) { throw new ArgumentException("Unable to get Claims Principal"); }
            return cachedPrincipal;
        }

        public  Task<bool> ValidateToken(string token, IdpSettings idpSettings)
        {
            //by pass use GetContextPrincipal to validate instead
            return Task.FromResult(true);
        }

        private async Task<GoogleTokenValidationPayload> ValidateToken(string token)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?access_token={token}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException("The token is invalid or expired.");
                }

                var payloadString = await response.Content.ReadAsStringAsync();
                var payload = JsonSerializer.Deserialize<GoogleTokenValidationPayload>(payloadString);
                return payload;
            }
            catch (Exception)
            {
                // Log exception or handle it as necessary
                throw new ArgumentException("The token is invalid or expired.");
            }
        }
    }

    public class GoogleTokenValidationPayload
    {
        [JsonPropertyName("azp")]
        public string Azp { get; set; }

        [JsonPropertyName("aud")]
        public string Aud { get; set; }

        [JsonPropertyName("sub")]
        public string Sub { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("exp")]
        public string Exp { get; set; }

        [JsonPropertyName("expires_in")]
        public string ExpiresIn { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("email_verified")]
        public string EmailVerified { get; set; }

        [JsonPropertyName("access_type")]
        public string AccessType { get; set; }
    }

}

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols; // Add this for MemoryCache
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthCodePKCEServerSide.TokenValidators
{


    public class MicrosoftTokenValidator : ICustomTokenHelper
    {
        private IdpSettings? _idpSetting;
        private static readonly MemoryCache DiscoveryDocumentCache = new MemoryCache(new MemoryCacheOptions());
        private static readonly TimeSpan DiscoveryDocumentCacheDuration = TimeSpan.FromMinutes(10);

        public async Task<bool> ValidateToken(string token, IdpSettings idpSetting)
        {
            _idpSetting = idpSetting;
            OpenIdConnectConfiguration? discoveryDocument;
            // Retry logic variables
            int retryCount = 0;
            int maxRetries = 1;
            while (retryCount <= maxRetries)
            {
                try
                {
                    bool validateResult = false;
                    var handler = new JwtSecurityTokenHandler();
                    var jwtSecurityToken = handler.ReadJwtToken(token);

                    // Extract the header values
                    var header = jwtSecurityToken.Header;
                    string kidInString = string.Empty;
                    //https://learn.microsoft.com/en-us/answers/questions/1359059/signature-validation-of-my-access-token-private-ke                  
                    if (header.TryGetValue("kid", out object? kid))
                    {
                        kidInString = (string)kid;
                    }
                    if (!DiscoveryDocumentCache.TryGetValue("DiscoveryDocument", out discoveryDocument))
                    {
                        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                            _idpSetting.Issuer + "/.well-known/openid-configuration",
                            new OpenIdConnectConfigurationRetriever(),
                            new HttpDocumentRetriever());

                        discoveryDocument = await configurationManager.GetConfigurationAsync();

                        DiscoveryDocumentCache.Set("DiscoveryDocument", discoveryDocument, DateTime.Now.Add(DiscoveryDocumentCacheDuration));
                    }
                    if (discoveryDocument == null) { return false; }

                    if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
                    foreach (SecurityKey signingKey in discoveryDocument.SigningKeys)
                    {
                        if (signingKey.KeyId == kidInString)
                        {
                            validateResult = true;
                        }
                    }
                    validateResult &= AdditionalTokenValidation(token, _idpSetting.Issuer);
                    return validateResult;
                }
                catch (SecurityTokenValidationException)
                {
                    retryCount++;
                    DiscoveryDocumentCache.Clear();
                    if (retryCount > maxRetries)
                    {
                        return false;
                    }
                }
            }
            return false;
        }



        public static bool AdditionalTokenValidation(string token, string expectedIssuer)
        {

            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("The token does not appear to be a valid JWT.");
            }

            var payload = parts[1];
            var payloadJson = Base64UrlDecode(payload);
            using (var jsonDoc = JsonDocument.Parse(payloadJson))
            {
                var expClaim = jsonDoc.RootElement.GetProperty("exp").GetInt64();
                var expiration = DateTimeOffset.FromUnixTimeSeconds(expClaim).UtcDateTime;
                var iss = jsonDoc.RootElement.GetProperty("iss").GetString();
                return iss == expectedIssuer && expiration > DateTime.UtcNow;
            }
        }


        private static string Base64UrlDecode(string input)
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 0: break;
                case 2: output += "=="; break;
                case 3: output += "="; break;
                default: throw new ArgumentException("Illegal base64url string!", nameof(input));
            }
            var converted = Convert.FromBase64String(output);
            return Encoding.UTF8.GetString(converted);
        }

        public async Task<ClaimsPrincipal> GetContextPrincipal(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, JwtBearerDefaults.AuthenticationScheme);
            return await Task.FromResult(new ClaimsPrincipal(identity));
        }
    }
}

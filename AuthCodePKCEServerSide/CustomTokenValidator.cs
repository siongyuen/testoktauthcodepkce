using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols; // Add this for MemoryCache
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;
using System.Security.Claims;

namespace AuthCodePKCEServerSide
{  

    public class CustomTokenValidator : ICustomTokenHelper
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
                

                    // Create the parameters used for validation
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,                        
                        IssuerSigningKeys = discoveryDocument.SigningKeys,
                        ValidateIssuer = false,
                        ValidIssuer = string.Empty ,
                        ValidateAudience = false,
                        ValidAudience = string.Empty,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5) // Allow a 5-minute clock skew for slight differences in server times
                    };

                    try
                    {
                     
                        var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);                    

                        validateResult = true; // Token is valid
                    }
                    catch (SecurityTokenValidationException)
                    {
                        // Token validation failed
                        validateResult = false;
                    }
                    catch (ArgumentException)
                    {
                        // Token was not well-formed
                        validateResult = false;
                    }

                    return validateResult;
                }
                catch (SecurityTokenValidationException)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        return false;
                    }
                }
            }
            return false; // This line is redundant due to the loop logic but added for clarity
        }
    }
}

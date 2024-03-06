using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Web;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthCodePKCEServerSide.Controllers
{
    public class OAuthController(IOptions<IdpSettings> idpSettings, ITokenCache tokenCache) : Controller
    {
        private readonly IdpSettings _idpSettings = idpSettings.Value;
        private readonly ITokenCache _tokenCache = tokenCache;

        [HttpPost]
        [Route("exchange-code")]
        public async Task<IActionResult> ExchangeCodeForToken([FromForm] CodeExchangeRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("redirect_uri", _idpSettings.RedirectUrl),
                    new KeyValuePair<string, string>("client_id", _idpSettings.ClientId),
                    new KeyValuePair<string, string>("code_verifier", request.CodeVerifier),
                    new KeyValuePair<string, string>("client_secret", _idpSettings.ClientSecret)
                });

                var response = await httpClient.PostAsync(_idpSettings.TokenEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to exchange code for token.");
                }

                var tokenResponse = await response.Content.ReadAsStringAsync();
                return Ok(tokenResponse);
            }
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromForm] RefreshTokenRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", request.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _idpSettings.ClientId),
                    new KeyValuePair<string, string>("client_secret", _idpSettings.ClientSecret)
                });

                var response = await httpClient.PostAsync(_idpSettings.TokenEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to refresh token.");
                }

                var tokenResponse = await response.Content.ReadAsStringAsync();
                return Ok(tokenResponse);
            }
        }
    }

    public class CodeExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

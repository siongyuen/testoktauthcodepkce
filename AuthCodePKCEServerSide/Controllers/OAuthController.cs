using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace AuthCodePKCEServerSide.Controllers
{
    public class OAuthController : Controller
    {
        private readonly IdpSettings _idpSettings;
        public OAuthController(IOptions<IdpSettings> idpSettings)
        {
            _idpSettings = idpSettings.Value;
        }
        [HttpPost]
        [Route("exchange-code")]
        public async Task<IActionResult> ExchangeCodeForToken([FromForm] CodeExchangeRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                var tokenEndpoint = _idpSettings.TokenEndpoint ; // Replace with your token endpoint
                var clientId = _idpSettings.ClientId ; // Replace with your client ID

                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", request.Code),
            new KeyValuePair<string, string>("redirect_uri", _idpSettings.RedirectUrl), // Replace with your redirect URI
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("code_verifier", request.CodeVerifier)
        });

                var response = await httpClient.PostAsync(tokenEndpoint, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to exchange code for token.");
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
}

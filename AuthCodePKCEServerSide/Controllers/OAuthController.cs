using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Web;

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
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", request.Code),
            new KeyValuePair<string, string>("redirect_uri", _idpSettings.RedirectUrl), // Replace with your redirect URI
            new KeyValuePair<string, string>("client_id", _idpSettings.ClientId ),          
            new KeyValuePair<string, string>("code_verifier", request.CodeVerifier)

            
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
    }
    public class CodeExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
    }
}

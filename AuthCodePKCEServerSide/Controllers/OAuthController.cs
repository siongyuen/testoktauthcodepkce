using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AuthCodePKCEServerSide.Controllers
{
    public class OAuthController : Controller
    {
        private const string OktaDomain = "https://dev-95411323.okta.com"; // Replace with your Okta domain

        [HttpPost]
        [Route("exchange-code")]
        public async Task<IActionResult> ExchangeCodeForToken([FromForm] CodeExchangeRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                var tokenEndpoint = $"{OktaDomain}/oauth2/default/v1/token"; // Replace with your token endpoint
                var clientId = "0oaefdvlfqiav6snB5d7"; // Replace with your client ID

                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", request.Code),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:12345/callback"), // Replace with your redirect URI
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("code_verifier", request.CodeVerifier)
        });

                var response = await httpClient.PostAsync(tokenEndpoint, content);

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
        public string Code { get; set; }
        public string CodeVerifier { get; set; }
    }
}

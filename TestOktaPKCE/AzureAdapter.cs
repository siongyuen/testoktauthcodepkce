using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static TestOktaPKCE.Models;

namespace TestOktaPKCE
{
    
public class AzureAdapter : IIdpAdapter
    {
        public string Domain { get; private set; }
        public string ClientId { get; private set; }
        public string RedirectUri { get; private set; }

        public string TenantId { get; private set; }


        public AzureAdapter( string clientId, string redirectUri, string tenantId)       {
    
            ClientId = clientId;
            RedirectUri = redirectUri;
            TenantId = tenantId;
        }

        public Tuple<string, string> StartAuthorization(string state)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            var authorizationRequest = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize?" +
                                       $"client_id={ClientId}" +
                                       "&response_type=code" +
                                       $"&scope={HttpUtility.UrlEncode("openid offline_access")}" +
                                       $"&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}" +
                                       $"&state={state}" + // Generate a secure random state
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }

        public async Task<Dictionary<string, string>> RefreshAccessToken(string refreshToken, HttpClient httpClient)
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("scope", "offline_access openid"),
                new KeyValuePair<string, string>("redirect", RedirectUri)
            });

            var response = await httpClient.PostAsync(tokenEndpoint, requestContent).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error while refreshing token.");
            }

            var jsonContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var tokens = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            return tokens;
        }
    }
}

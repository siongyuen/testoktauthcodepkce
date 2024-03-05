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

    public class GoogleAdapter : IIdpAdapter
    {
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; } // Required for token exchange
        public string RedirectUri { get; private set; }

        public GoogleAdapter(string clientId, string clientSecret, string redirectUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri;
        }

        public Tuple<string, string> StartAuthorization(string state)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            var authorizationRequest = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                                       $"client_id={ClientId}" +
                                       "&response_type=code" +
                                       "&scope=" + HttpUtility.UrlEncode("openid email profile") +
                                       $"&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}" +
                                       $"&state={state}" +
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}" +
                                       "&access_type=offline&prompt=consent";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }

        public async Task<Dictionary<string, string>> RefreshAccessToken(string refreshToken, HttpClient httpClient)
        {
            var tokenEndpoint = "https://oauth2.googleapis.com/token";
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("scope", "openid email profile"),
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

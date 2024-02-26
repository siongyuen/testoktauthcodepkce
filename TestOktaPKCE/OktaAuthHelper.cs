using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace TestOktaPKCE
{
    public class OktaAuthHelper
    {
        private const string Scope = "openid offline_access"; // Include offline_access to request a refresh token
        private static readonly HttpClient httpClient = new HttpClient();

        public static Tuple<string, string> StartAuthorization(string oktaDomain, string clientId, string redirectUri, string state)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            var authorizationRequest = $"{oktaDomain}/oauth2/default/v1/authorize?" +
                                       $"client_id={clientId}" +
                                       "&response_type=code" +
                                       $"&scope={HttpUtility.UrlEncode(Scope)}" +
                                       $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
                                       $"&state={state}" + // Generate a secure random state
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }

        // Method to refresh the access token
        public static async Task<Dictionary<string, string>> RefreshAccessToken(string oktaDomain, string clientId, string refreshToken, string redirect)
        {
            var tokenEndpoint = $"{oktaDomain}/oauth2/default/v1/token";
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("scope", "offline_access openid"),
                new KeyValuePair<string, string>("redirect", redirect)

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

        public static async Task<string> SendCodeToServerAsync(string serverEndpoint, string code, string codeVerifier)
        {
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("Code", code),
        new KeyValuePair<string, string>("CodeVerifier", codeVerifier)
    });

            var response = await httpClient.PostAsync(serverEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error while sending code to server.");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}

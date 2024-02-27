using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace TestOktaPKCE
{
    public class AuthHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static Tuple<string, string> StartAuthorization(Models.IIdpAdapter idpConfig, string state)
        {
            return idpConfig.StartAuthorization(state);
        }

        public static async Task<Dictionary<string, string>> RefreshAccessToken(Models.IIdpAdapter idpConfig, string refreshToken)
        {
            return await idpConfig.RefreshAccessToken(refreshToken, httpClient ).ConfigureAwait(false);
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

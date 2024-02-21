using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace TestOktaPKCE
{
    public class OktaAuthHelper
    {
        private const string Scope = "openid offline_access"; // Include offline_access to request a refresh token
        private static readonly HttpClient httpClient = new HttpClient();

        public static Tuple<string, string> StartAuthorization(string oktaDomain, string clientId, string redirectUri)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            var authorizationRequest = $"{oktaDomain}/oauth2/default/v1/authorize?" +
                                       $"client_id={clientId}" +
                                       "&response_type=code" +
                                       $"&scope={HttpUtility.UrlEncode(Scope)}" +
                                       $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
                                       "&state=state-12345" + // Generate a secure random state
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }




    }
}

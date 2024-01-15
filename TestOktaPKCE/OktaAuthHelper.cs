using System;
using System.Diagnostics;
using System.Web;
using TestOktaPKCE;

namespace TestOktaPKCE
{


public class OktaAuthHelper
{

    private const string Scope = "openid";

        public static Tuple<string, string> StartAuthorization(string oktaDomain, string clientId, string redirectUri)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            // Store the codeVerifier in a secure place to use it later in the token exchange

            var authorizationRequest = $"{oktaDomain}/oauth2/default/v1/authorize?" +
                                       $"client_id={clientId}" +
                                       "&response_type=code" +
                                       $"&scope={HttpUtility.UrlEncode(Scope)}" +
                                       $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
                                       "&state=state-12345" + // You should generate a secure random state                                   
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }


    }

}
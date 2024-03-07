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
        public string RedirectUri { get; private set; }

        public GoogleAdapter(string clientId, string redirectUri)
        {
            ClientId = clientId;  
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


    }
}

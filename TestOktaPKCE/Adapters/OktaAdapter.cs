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
    
public class OktaAdapter : IIdpAdapter
    {
        public string Domain { get; private set; }
        public string ClientId { get; private set; }
        public string RedirectUri { get; private set; }

        public OktaAdapter(string domain, string clientId, string redirectUri)
        {
            Domain = domain;
            ClientId = clientId;
            RedirectUri = redirectUri;
        }

        public Tuple<string, string> StartAuthorization(string state)
        {
            var codeVerifier = PKCEHelper.GenerateCodeVerifier();
            var codeChallenge = PKCEHelper.GenerateCodeChallenge(codeVerifier);

            var authorizationRequest = $"{Domain}/oauth2/default/v1/authorize?" +
                                       $"client_id={ClientId}" +
                                       "&response_type=code" +
                                       $"&scope={HttpUtility.UrlEncode("openid offline_access")}" +
                                       $"&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}" +
                                       $"&state={state}" + // Generate a secure random state
                                       "&code_challenge_method=S256" +
                                       $"&code_challenge={codeChallenge}";

            return Tuple.Create(codeVerifier, authorizationRequest);
        }   
    }
}

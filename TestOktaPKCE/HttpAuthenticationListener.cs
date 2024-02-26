using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms; // If you need to interact with UI components directly
namespace TestOktaPKCE
{

    public class HttpAuthenticationListener
    {
        private HttpListener httpListener;
        private readonly string redirectUri;
        private string _codeVerifier;
        public event EventHandler<string> AccessTokenObtained;
        private string _expectedState;

        public void SetCodeVerifier(string codeVerifier)
        {
            _codeVerifier = codeVerifier;
        }

        public void SetExpectedState(string state)
        { _expectedState = state; }

        public HttpAuthenticationListener(string redirectUri)
        {
            this.redirectUri = redirectUri;            
            httpListener = new HttpListener();
        }

        public void Start()
        {
            httpListener.Prefixes.Add(redirectUri + "/");
            httpListener.Start();
            Task.Run(() => ListenForCallback());
        }

        private async Task ListenForCallback()
        {
            while (httpListener.IsListening)
            {
                var context = await httpListener.GetContextAsync();
                var request = context.Request;

                if (request.Url.AbsolutePath == "/callback")
                {
                    // Process the callback
                    await ProcessCallback(request);
                }
            }
        }

        private async Task ProcessCallback(HttpListenerRequest request)
        {
            NameValueCollection query = request.QueryString;
            string code = query["code"];
            string state = query["state"];

            
            if (state != _expectedState)
            {
                Console.WriteLine("State value did not match expected value.");
                return;
            }

            string tokenResponseInString = await OktaAuthHelper.SendCodeToServerAsync("https://localhost:7064/exchange-code", code, _codeVerifier);
            var tokenResponse = JsonConvert.DeserializeObject<Models.TokenResponse>(tokenResponseInString);

            AccessTokenObtained?.Invoke(this, tokenResponse.AccessToken);
        }
    }
}
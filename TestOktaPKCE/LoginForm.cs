
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Text;

namespace TestOktaPKCE
{
    public partial class LoginForm : Form
    {
        private HttpListener httpListener;
        private const string RedirectUri = "http://localhost:12345/callback";
        private string _codeVerifier;
        private const string OktaDomain = "https://dev-95411323.okta.com"; // Replace with your Okta domain
        private const string ClientId = "0oaefdvlfqiav6snB5d7"; // Replace with your client ID
        private static readonly HttpClient httpClient = new HttpClient();

        public LoginForm()
        {
            InitializeComponent();
            StartHttpListener();
        }

        private void StartHttpListener()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(RedirectUri + "/");
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
                    // Extract the code and state from the query string
                    NameValueCollection query = request.QueryString;
                    string code = query["code"];
                    string state = query["state"];                           


                    // Validate the state parameter for CSRF protection
                    string expectedState = "state-12345"; // Replace with your actual state value
                    if (state != expectedState)
                    {
                        // Handle state mismatch (potential CSRF attack)
                        Console.WriteLine("State value did not match expected value.");
                        return;
                    }

                    // server side

                    string response = await SendCodeToServerAsync("https://localhost:7064/exchange-code", code, _codeVerifier);


                   
                    

            

                }
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var result = OktaAuthHelper.StartAuthorization(OktaDomain, ClientId, RedirectUri);
                _codeVerifier = result.Item1;
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }



        public async Task<string> SendCodeToServerAsync(string serverEndpoint, string code, string codeVerifier)
        {
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("code_verifier", codeVerifier)
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

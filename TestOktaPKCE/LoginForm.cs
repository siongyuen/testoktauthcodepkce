
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
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
                HttpListenerResponse clientSideResponse = context.Response;

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


                    // Exchange code for tokens
                    string tokenEndpoint = $"{OktaDomain}/oauth2/default/v1/token"; // Replace with your Okta token endpoint                   

                    string redirectUri = "http://localhost:12345/callback"; // Replace with your redirect URI


                    using (var httpClient = new HttpClient())
                    {
                        var requestBody = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("redirect_uri", redirectUri),
                        new KeyValuePair<string, string>("client_id", ClientId),
                        new KeyValuePair<string, string>("code_verifier", _codeVerifier)

        });

                        var response = await httpClient.PostAsync(tokenEndpoint, requestBody);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            var tokens = JsonSerializer.Deserialize<JsonElement>(responseContent);

                            // Use the tokens as needed
                            string accessToken = tokens.GetProperty("access_token").GetString();
                            string idToken = tokens.GetProperty("id_token").GetString();
                            clientSideResponse.Redirect("http://com.okta.dev-95411323:");
                            
                            // Signal the rest of your application that auth was successful
                            // For example, update the UI or store the tokens securely

                            // Close the listener

                            MessageBox.Show("Token obtained successfully.");
                            MessageBox.Show("Token Validated =" + ValidateToken(accessToken, OktaDomain).ToString());
                            
                        }
                        else
                        {
                            // Handle token exchange failure
                            MessageBox.Show("Failed to obtained token.");
                        }
                    }

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

        public bool ValidateToken(string token, string oktaDomain)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonWebKeySet = GetJsonWebKeySetAsync().Result ; // Implement this to get JWKS from Okta
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jsonWebKeySet.Keys,
                ValidateIssuer = true,
                ValidIssuer = $"{oktaDomain}/oauth2/default",
                ValidateAudience = false,                
                ValidateLifetime = true
            };

            try
            {
                tokenHandler.ValidateToken(token, parameters, out var validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<JsonWebKeySet> GetJsonWebKeySetAsync()
        {
           var httpClient = new HttpClient();
            var jwksUri = $"{OktaDomain}/oauth2/default/v1/keys";
            var response = await httpClient.GetAsync(jwksUri);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonWebKeySet = new JsonWebKeySet(jsonResponse);

            return jsonWebKeySet;
        }
    }
}

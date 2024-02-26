
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Web;
using System.Net.Sockets;

namespace TestOktaPKCE
{
    public partial class LoginForm : Form
    {
        
        private const string RedirectUri = "http://localhost:12345/callback";
        private string _codeVerifier;
        private const string OktaDomain = "https://dev-95411323.okta.com"; // Replace with your Okta domain
        private const string ClientId = "0oaefdvlfqiav6snB5d7"; // Replace with your client ID
        private static readonly HttpClient httpClient = new HttpClient();
        private string _accessToken;
        private string _refreshToken;
        private HttpAuthenticationListener httpListener;


        public LoginForm()
        {
            InitializeComponent();
            httpListener = new HttpAuthenticationListener(RedirectUri);
            httpListener.AccessTokenObtained += HttpListener_AccessTokenObtained;
            httpListener.Start();            
        }
        

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void HttpListener_AccessTokenObtained(object sender, string accessToken)
        {
            MessageBox.Show("Access Token Obtain : " + accessToken);
            // Update UI or internal state as necessary
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var result = OktaAuthHelper.StartAuthorization(OktaDomain, ClientId, RedirectUri);
                httpListener.SetCodeVerifier(result.Item1);                
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var result = GetWeatherForecast();

                MessageBox.Show($"Temperature from API is : {result.FirstOrDefault().TemperatureC.ToString()}");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }



    

        public IEnumerable<Models.WeatherForecast> GetWeatherForecast()
        {

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            // Send the GET request
            HttpResponseMessage response = httpClient.GetAsync("https://localhost:7064/weatherforecast").Result;
            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                var forecasts = JsonConvert.DeserializeObject<IEnumerable<Models.WeatherForecast>>(json); // Assuming WeatherForecast is your model class
                                                                                                   // Process the data
                return forecasts;
            }
            else
            {
                return null;
            }
           ;
        }
       
        private void button3_Click(object sender, EventArgs e)
        {
         
            var response = OktaAuthHelper.RefreshAccessToken(OktaDomain, ClientId, _refreshToken, RedirectUri).Result;
            response.TryGetValue("access_token", out string accessToken);
            response.TryGetValue("refresh_token", out _refreshToken);
            MessageBox.Show($"refreshed access token : {accessToken}");
        }
    }

   
}

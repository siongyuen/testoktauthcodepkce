
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Forms;
using System.Configuration;

namespace TestOktaPKCE
{
    public partial class LoginForm : Form
    {

        private readonly string RedirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        private readonly string OktaDomain = ConfigurationManager.AppSettings["OktaDomain"];
        private readonly string OktaClientId = ConfigurationManager.AppSettings["OktaClientId"];
        private static readonly HttpClient httpClient = new HttpClient();
        private string _accessToken;
        
        private readonly HttpAuthenticationListener httpListener;


        public LoginForm()
        {
            InitializeComponent();
            httpListener = new HttpAuthenticationListener(RedirectUri);
            httpListener.TokenResponseObtained += HttpListener_AccessTokenObtained;
            httpListener.Start();            
        }
        

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void HttpListener_AccessTokenObtained(object sender, string  accessToken)
        {
            MessageBox.Show("Access Token Obtain : " + accessToken);
            _accessToken = accessToken;  

            // Update UI or internal state as necessary
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string state = "random";
                var oktaConfig = new OktaAdapter(OktaDomain, OktaClientId, RedirectUri);
                var result = AuthHelper.StartAuthorization(oktaConfig, state);
                httpListener.SetCodeVerifier(result.Item1);
                httpListener.SetExpectedState(state);
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
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
       
    

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                string state = "random";
                var azureAdapter = new AzureAdapter ( "0a2a1325-4bc0-4b4c-bea6-1af3fe408392", RedirectUri ,"81180712-5369-494f-9d7f-514eccf5e9f8");
                var result = AuthHelper.StartAuthorization(azureAdapter, state);
                httpListener.SetCodeVerifier(result.Item1);
                httpListener.SetExpectedState(state);
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _accessToken = AuthHelper.RefreshToken("https://localhost:7064/refresh-token", ConfigurationManager.AppSettings["EmailForRefreshToken"]).Result;

            MessageBox.Show($"refreshed access token : {_accessToken}");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                string state = "random";
                var idpConfig = new GoogleAdapter (ConfigurationManager.AppSettings["GoogleClientId"], RedirectUri);
                var result = AuthHelper.StartAuthorization(idpConfig, state);
                httpListener.SetCodeVerifier(result.Item1);
                httpListener.SetExpectedState(state);
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

       
        private void button5_Click_1(object sender, EventArgs e)
        {
            try
            {
                string state = "random";
                var idpConfig = new GoogleAdapter (ConfigurationManager.AppSettings["GoogleClientId"], RedirectUri );
                var result = AuthHelper.StartAuthorization(idpConfig, state);
                httpListener.SetCodeVerifier(result.Item1);
                httpListener.SetExpectedState(state);
                var authorizationRequest = result.Item2;
                System.Diagnostics.Process.Start(authorizationRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        } 
    }

   
}

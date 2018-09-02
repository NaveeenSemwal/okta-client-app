using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace client_okta
{
    public class OktaTokenService : ITokenService
    {
        private OktaToken token = new OktaToken();
        private readonly IOptions<OktaSettings> oktaSettings;

        

        public OktaTokenService(IOptions<OktaSettings> oktaSettings)
        {
            this.oktaSettings = oktaSettings;
        }

        public async Task<string> GetToken()
        {
            if (!this.token.IsValidAndNotExpiring)
            {
               await this.GetNewAccessToken();
            }
            return token.AccessToken;
        }

        private async Task<OktaToken> GetNewAccessToken()
        {
            var token = new OktaToken();
            var client = new HttpClient();
            var client_id = this.oktaSettings.Value.ClientId;
            var client_secret = this.oktaSettings.Value.ClientSecret;
            var clientCreds = System.Text.Encoding.UTF8.GetBytes($"{client_id}:{client_secret}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(clientCreds));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var postMessage = new Dictionary<string, string>();
            postMessage.Add("grant_type", "client_credentials");
            postMessage.Add("scope", "access_token");
            var request = new HttpRequestMessage(HttpMethod.Post, this.oktaSettings.Value.TokenUrl)
            {
                Content = new FormUrlEncodedContent(postMessage)
            };

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                this.token = JsonConvert.DeserializeObject<OktaToken>(json);
                this.token.ExpiresAt = DateTime.Now.AddHours(1);
            }
            else
            {
                throw new ApplicationException("Unable to retrieve access token from Okta");
            }
            return token;
        }
    }

    class OktaToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        public DateTime ExpiresAt { get; set; }

        public string Scope { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        public bool IsValidAndNotExpiring
        {
            get
            {
                return !String.IsNullOrEmpty(this.AccessToken) &&
          this.ExpiresAt > DateTime.Now.AddHours(1);
            }
        }
    }

    public class SimpleApiService : IApiService
    {
        private HttpClient client = new HttpClient();
        private readonly ITokenService tokenService;
        public SimpleApiService(ITokenService tokenService)
        {
            this.tokenService = tokenService;
        }

        public async Task<IList<string>> GetValues()
        {
            List<string> values = new List<string>();
            var token = await tokenService.GetToken();
            client.DefaultRequestHeaders.Add("token", "Bearer " + token);


            OktaServiceReference.OktaServiceClient clientOkta = new OktaServiceReference.OktaServiceClient();
            using (new OperationContextScope(clientOkta.InnerChannel))
            {

                // https://blogs.msdn.microsoft.com/wsdevsol/2014/02/07/adding-custom-messageheader-and-http-header-to-a-wcf-method-call-in-a-windows-store-app-or-windows-phone-app/
                // Add a SOAP Header to an outgoing request
                MessageHeader aMessageHeader = MessageHeader.CreateHeader("UserInfo", "http://tempuri.org","Naveen");
                OperationContext.Current.OutgoingMessageHeaders.Add(aMessageHeader);

                // Add a HTTP Header to an outgoing request
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                requestMessage.Headers["Authorization"] = token;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

               
               await clientOkta.DoWorkAsync();
            }



            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("http://localhost:51026/api/values");

            if (res.IsSuccessStatusCode)
            {
                var json = res.Content.ReadAsStringAsync().Result;
                values = JsonConvert.DeserializeObject<List<string>>(json);
            }
            else
            {
                values = new List<string> { res.StatusCode.ToString(), res.ReasonPhrase };
            }
            return values;
        }
    }
}


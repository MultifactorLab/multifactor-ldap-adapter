//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services.MultiFactorApi
{

    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    public class MultiFactorApiClient
    {
        readonly JsonSerializerOptions _serializerOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        public MultiFactorApiClient(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Try all API urls to verify second factor
        /// </summary>
        /// <param name="request">Data for second facor request</param>
        /// <returns></returns>
        public async Task<MultiFactorAccessResponse> FaultTolerantSecondFactorRequest(MultiFactorAccessRequest request)
        {
            MultiFactorAccessResponse response;
            //make sure we can communicate securely
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            ServicePointManager.DefaultConnectionLimit = 100;

            var httpClient = _httpClientFactory.CreateClient(nameof(MultiFactorApiClient));

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", request.Auth);
            var identity = new MultiFactorIdentityDto { Identity = request.Identity };

            foreach (var url in request.ApiUrls)
            {
                response = await SendRequest(httpClient, url, identity);

                if (response != MultiFactorAccessResponse.Empty)
                    return response;
                _logger.Warning("Failed to send request to {url:l}, try next host", url);
            }

            // bypass\reject only after all urls are tried
            if (request.BypassSecondFactor)
            {
                _logger.Warning("Bypass second factor");
                return MultiFactorAccessResponse.Bypass;
            }
            _logger.Error("All Multifactor API hosts unreachable {ApiUrls}", request.ApiUrls);
            return MultiFactorAccessResponse.Empty;
        }

        private async Task<MultiFactorAccessResponse> SendRequest(HttpClient httpClient, string url, MultiFactorIdentityDto identity)
        {
            try
            {
                var json = JsonSerializer.Serialize(identity, _serializerOptions);
                _logger.Debug("Sending request to API {url:l}: {@jsonPayload}", url, json);

                StringContent jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, $"{url}/access/requests/la")
                {
                    Content = jsonContent
                };
                var res = await httpClient.SendAsync(message);
                res.EnsureSuccessStatusCode();
                var jsonResponse = await res.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<MultiFactorApiResponse<MultiFactorAccessResponse>>(jsonResponse, _serializerOptions);
                _logger.Debug("Received response from API {url:l}: {@response}", url, response);

                if (!response.Success)
                {
                    _logger.Warning("Got unsuccessful response from API {url:l}: {@response}", url, response);
                }

                return response.Model;
            }
            catch (TaskCanceledException tce)
            {
                _logger.Error(tce, "Multifactor API host unreachable {url:l}: Http Timeout!", url);
                return MultiFactorAccessResponse.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Multifactor API host unreachable {url:l}: {Message}", url, ex.Message);
                return MultiFactorAccessResponse.Empty;
            }
        }

    }
}

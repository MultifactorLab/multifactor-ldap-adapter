//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md


using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using System.Text;

namespace MultiFactor.Ldap.Adapter.Services
{
    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    public class MultiFactorApiClient
    {
        private Configuration _configuration;
        private ILogger _logger;

        public MultiFactorApiClient(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Authenticate(string userName)
        {
            var url = _configuration.ApiUrl + "/access/requests/la";
            var payload = new
            {
                Identity = userName,
            };

            var response = SendRequest(url, payload);

            if (response == null)
            {
                return false;
            }

            if (response.Granted && !response.Bypassed)
            {
                _logger.Information($"Second factor for user '{userName}' verified successfully");
            }

            return response.Granted;
        }

        private MultiFactorAccessRequest SendRequest(string url, object payload)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 100;

                var json = JsonConvert.SerializeObject(payload);

                _logger.Debug($"Sending request to API: {json}");

                var requestData = Encoding.UTF8.GetBytes(json);
                byte[] responseData = null;

                //basic authorization
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration.NasIdentifier + ":" + _configuration.MultiFactorSharedSecret));

                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", "Basic " + auth);

                    if (!string.IsNullOrEmpty(_configuration.ApiProxy))
                    {
                        _logger.Debug("Using proxy " + _configuration.ApiProxy);
                        web.Proxy = new WebProxy(_configuration.ApiProxy);
                    }

                    responseData = web.UploadData(url, "POST", requestData);
                }

                json = Encoding.UTF8.GetString(responseData);

                _logger.Debug($"Received response from API: {json}");

                var response = JsonConvert.DeserializeObject<MultiFactorApiResponse<MultiFactorAccessRequest>>(json);

                if (!response.Success)
                {
                    _logger.Warning($"Got unsuccessful response from API: {json}");
                }

                return response.Model;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Multifactor API host unreachable {url}: {ex.Message}");

                if (_configuration.BypassSecondFactorWhenApiUnreachable)
                {
                    _logger.Warning("Bypass second factor");
                    return MultiFactorAccessRequest.Bypass;
                }

                return null;
            }
        }
    }

    public class MultiFactorApiResponse<TModel>
    {
        public bool Success { get; set; }

        public TModel Model { get; set; }
    }

    public class MultiFactorAccessRequest
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string ReplyMessage { get; set; }
        public bool Bypassed { get; set; }

        public bool Granted => Status == "Granted";

        public static MultiFactorAccessRequest Bypass
        {
            get
            {
                return new MultiFactorAccessRequest { Status = "Granted", Bypassed = true };
            }
        } 
    }
}

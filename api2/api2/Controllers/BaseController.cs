using api2.ConsulInfra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;

namespace api2.Controllers
{
    public class BaseController : ControllerBase
    {
        protected readonly IConsulConsumptionService _consulConsumptionService;
        protected readonly HttpClient _apiClient;
        public int _currentConfigIndex = 0;
        protected AsyncRetryPolicy _serverRetryPolicy;
        public Dictionary<string, List<Uri>> _servers;
        public BaseController(IConsulConsumptionService consulConsumptionService)
        {
            _consulConsumptionService = consulConsumptionService;
            _apiClient = new HttpClient();
            _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        protected async Task InitializeAsync(string servicename)
        {
            _servers = await _consulConsumptionService.CheckServerHealth(servicename);
            var retries = _servers[servicename].Count * 2 - 1;
            _serverRetryPolicy = Polly.Policy.Handle<HttpRequestException>().RetryAsync(retries, (exception, retryCount) =>
            {
                ChooseNextServer(retryCount);
            });
        }

        protected void ChooseNextServer(int retryCount)
        {
            if (retryCount % 2 == 0)
            {
                Console.WriteLine("Trying next server...\n");
                _currentConfigIndex++;

                if (_currentConfigIndex >= 2)
                    _currentConfigIndex = 0;
            }
        }
    }
}

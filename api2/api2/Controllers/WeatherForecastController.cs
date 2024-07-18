using api2.ConsulInfra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Polly.Retry;
using Polly;
using Consul;
using System.Net.Http.Headers;

namespace api2.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : BaseController
    {
        consulservices _srv;
        public WeatherForecastController(IOptions<consulservices> serv, IConsulConsumptionService consulConsumptionService) : base(consulConsumptionService)
        {
            _srv = serv.Value;
            InitializeAsync(_srv.patientservice).GetAwaiter().GetResult();
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public virtual async Task<string> GetAsync()
        {
            await InitializeAsync(_srv.patientservice);
            var _serverUrls = _consulConsumptionService.GetURLs(_srv.patientservice);
            return await _serverRetryPolicy.ExecuteAsync(async () =>
            {
                var serverUrl = _serverUrls[_srv.patientservice][_currentConfigIndex];
                _currentConfigIndex++;
                var requestPath = $"{serverUrl}api/WeatherForecast/testmethod";
                var response = await _apiClient.GetAsync(requestPath).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);            
                return content;
            });
        }
        [HttpGet(Name = "testmethod")]
        public virtual async Task<IEnumerable<WeatherForecast>> testmethod()
        {
            return await Task.FromResult(new List<WeatherForecast>());
        }
    }
}

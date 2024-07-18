using Consul;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace api2.ConsulInfra
{
    public interface IConsulConsumptionService
    {
        Task<Dictionary<string, List<Uri>>> CheckServerHealth(string serviceName);
        Dictionary<string, List<Uri>> GetURLs(string key);
    }
    public class ConsulConsumptionService : IConsulConsumptionService
    {
        private readonly ConsulClient _consulClient;
        private readonly HttpClient _apiClient;
        private readonly Dictionary<string, List<Uri>> _serverUrls;
        ConsulConfig _config;
        consulservices _serv;
        public ConsulConsumptionService(IOptions<ConsulConfig> options, IOptions<consulservices> serv)
        {
            _config = options.Value;
            _serv = serv.Value;
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

            _consulClient = new ConsulClient(c =>
            {
                c.Address = new Uri(_config.Address);
            });

            _apiClient = new HttpClient();
            _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _serverUrls = new Dictionary<string, List<Uri>>();
        }
        public async Task<Dictionary<string, List<Uri>>> CheckServerHealth(string serviceName)
        {
            var checks = await _consulClient.Health.Service(serviceName);

            foreach (var entry in checks.Response)
            {
                var check = entry.Checks.SingleOrDefault(c => c.ServiceName == serviceName);
                if (check == null) continue;
                var isPassing = check.Status == HealthStatus.Passing;
                var serviceUri = new Uri($"{entry.Service.Address}:{entry.Service.Port}");
                if (check != null)
                {
                    if (!_serverUrls.ContainsKey(serviceName))
                    {
                        _serverUrls[serviceName] = new List<Uri>();
                    }
                    if (!_serverUrls[serviceName].Contains(serviceUri))
                    {
                        _serverUrls[serviceName].Add(serviceUri);
                    }
                }
                else
                {
                    if (_serverUrls.ContainsKey(serviceName) && _serverUrls[serviceName].Contains(serviceUri))
                    { 
                        _serverUrls[serviceName].Remove(serviceUri);
                    }
                }
            }
            return _serverUrls;


        }
        public Dictionary<string, List<Uri>> GetURLs(string key)
        {
            return _serverUrls;       
        }
    }
}

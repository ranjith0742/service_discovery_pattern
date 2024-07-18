using Consul;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;
using System.Net;

namespace api2.ConsulInfra
{
    public class ConsulRegistrationService : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;
        private readonly IConsulClient _consulClient;
        private readonly IOptions<ConsulConfig> _consulConfig;
        private readonly IServer _server;
        private string _registrationID;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostApplicationLifetime _appLifetime;
        public ConsulRegistrationService(IConsulClient consulClient, IOptions<ConsulConfig> consulConfig, IServer server, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IHostApplicationLifetime hostApplicationLifetime)
        {
            _server = server;
            _consulConfig = consulConfig;
            _consulClient = consulClient;
            _env = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _appLifetime = hostApplicationLifetime;


        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _appLifetime.ApplicationStarted.Register(OnApplicationStarted);

        }

        private async void OnApplicationStarted()
        {
            try
            {
                var addresses = _server.Features.Get<IServerAddressesFeature>().Addresses;

                if (addresses != null && addresses.Any())
                {
                    foreach (var address in addresses)
                    {
                        try
                        {
                            string ipAddress = GetMachineIPAddress();
                            var uri = new Uri(address.Replace("*", ipAddress));
                            _registrationID = $"{_consulConfig.Value.ServiceID}-{uri.Port}";

                            var registration = new AgentServiceRegistration()
                            {
                                ID = _registrationID,
                                Name = _consulConfig.Value.ServiceName,
                                Address = $"{uri.Scheme}://{uri.Host}",
                                Port = uri.Port,
                                Tags = new[] { "Courses", "School" },
                                Check = new AgentServiceCheck()
                                {
                                    HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}/api/health/status",
                                    Timeout = TimeSpan.FromSeconds(5),
                                    Interval = TimeSpan.FromSeconds(5),
                                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(9),
                                }
                            };

                            await _consulClient.Agent.ServiceDeregister(registration.ID, _cts.Token);
                            await _consulClient.Agent.ServiceRegister(registration, _cts.Token);
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("No server addresses found.");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
        }
        private string GetMachineIPAddress()
        {
            // Get all IP addresses associated with the machine
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                // Check if it's a usable IPv4 address (not loopback or IPv6)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            // If no suitable IP found, return localhost or handle error
            return "localhost";
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            try
            {
                await _consulClient.Agent.ServiceDeregister(_registrationID, cancellationToken);
            }
            catch (Exception ex)
            {

            }
        }
    }
}

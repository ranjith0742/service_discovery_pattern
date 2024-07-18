using api2.ConsulInfra;

namespace api2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();       
                }).ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostedService, ConsulRegistrationService>();
                    // Add other services
                });
    }
}

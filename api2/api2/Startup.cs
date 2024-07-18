using api2.ConsulInfra;
using Consul;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace api2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllers();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Configure<ConsulConfig>(_configuration.GetSection("consulConfig"));
            services.Configure<consulservices>(_configuration.GetSection("consulservices"));
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = _configuration["consulConfig:address"];
                consulConfig.Address = new Uri(address);
                consulConfig.Token = "your_master_token_here";
            }));

            services.AddSingleton<IConsulConsumptionService, ConsulConsumptionService>();
            services.AddSingleton<IHostedService, ConsulRegistrationService>();
     
            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }));
            services.AddHealthChecks();
            services.AddSwaggerGen();

            //services.AddHostedService<ConsulRegistrationService>();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHealthChecks("/health");
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "RC Web API"); });
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}

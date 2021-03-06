using Archimedes.Library.Candles;
using Archimedes.Library.Domain;
using Archimedes.Library.Message;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Strategy.Http;
using Archimedes.Service.Strategy.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Archimedes.Service.Strategy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.GetSection("AppSettings").Get<Config>();
            services.AddLogging();
            services.AddSignalR();

            services.AddControllers();
            services.AddHttpClient<IHttpRepositoryClient, HttpRepositoryClient>();
            services.Configure<Config>(Configuration.GetSection("AppSettings"));

            services.AddTransient<IStrategySubscriber, StrategySubscriber>();
            services.AddTransient<IStrategyPublisher, StrategyPublisher>();
            services.AddTransient<IPriceLevelStrategy, PriceLevelStrategy>();

            services.AddTransient<IPivotLevelStrategyHigh, PivotLevelStrategyHigh>();
            services.AddTransient<IPivotLevelStrategyLow, PivotLevelStrategyLow>();

            services.AddTransient<ICandleHistoryLoader, CandleHistoryLoader>();
            services.AddTransient<IStrategyConsumer>(x => new StrategyConsumer(config.RabbitHost, config.RabbitPort, config.RabbitExchange,"StrategyRequestQueue"));

            services.AddTransient<IProducerFanout<PriceLevelMessage>>(x => new ProducerFanout<PriceLevelMessage>(config.RabbitHost, config.RabbitPort));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<StrategyHub>("/hubs/strategy");
                endpoints.MapHub<PriceLevelHub>("/hubs/price-level");
            });
        }
    }
}

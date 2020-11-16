using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Message.Dto;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Strategy
{
    public class PriceSubscriptionService : IHostedService
    {
        private readonly ILogger<PriceSubscriptionService> _logger;
        private readonly HubConnection _connection;
        private readonly Config _config;
        private readonly ITradeConsumer _consumer;

        public PriceSubscriptionService(ILogger<PriceSubscriptionService> logger,IOptions<Config> config, ITradeConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
            _config = config.Value;
            _connection = new HubConnectionBuilder().WithUrl($"{config.Value.RepositoryUrl}hubs/price")
                .Build();

            _connection.On<PriceDto>("Update", price => { Update(price); });

        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Initialise Price hub {_config.RepositoryUrl}hubs/price");

            while (true)
            {
                try
                {
                    await _connection.StartAsync(cancellationToken);
                    break;
                }
                catch(Exception e)
                {
                    _logger.LogWarning($"Error from connection start: {e.Message}");
                    await Task.Delay(10000, cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _connection.DisposeAsync();
        }

        public Task Update(PriceDto price)
        {
            _logger.LogInformation($"Update received from one of the repo apis {price}");
            _consumer.ProcessTradeCalculations(price);

            return Task.CompletedTask;
        }
    }
}

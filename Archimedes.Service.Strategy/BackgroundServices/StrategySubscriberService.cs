using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class StrategySubscriberService : BackgroundService
    {
        private readonly IStrategySubscriber _strategySubscriber;
        private readonly ILogger<StrategySubscriberService> _logger;

        public StrategySubscriberService(IStrategySubscriber strategySubscriber, ILogger<StrategySubscriberService> logger)
        {
            _strategySubscriber = strategySubscriber;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _strategySubscriber.Consume(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unknown error found in StrategyBackgroundService: {e.Message} {e.StackTrace}");
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }
    }
}
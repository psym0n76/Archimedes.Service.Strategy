using System;
using System.Threading;
using Archimedes.Library.Message;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Strategy.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Archimedes.Service.Strategy
{
    public class StrategySubscriber : IStrategySubscriber
    {
        private readonly ILogger<StrategySubscriber> _logger;
        private readonly IStrategyConsumer _consumer;
        private readonly ICandleLoader _loader;
        private readonly IPriceLevelStrategy _priceLevelStrategy;
        private readonly IHttpRepositoryClient _client;

        public StrategySubscriber(ILogger<StrategySubscriber> logger, IStrategyConsumer consumer, ICandleLoader loader,
            IPriceLevelStrategy priceLevelStrategy, IHttpRepositoryClient client)
        {
            _logger = logger;
            _consumer = consumer;
            _loader = loader;
            _priceLevelStrategy = priceLevelStrategy;
            _client = client;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        public void Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(cancellationToken);
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            var message = JsonConvert.DeserializeObject<StrategyMessage>(args.Message);

            _logger.LogInformation($"Received from StrategyResponseQueue:: {message}");
            RunStrategies(message);
        }

        private async void RunStrategies(StrategyMessage message)
        {
            try
            {
                var strategies = await _client.GetStrategiesByGranularityMarket(message.Market, message.Granularity);
                var candles = await _loader.Load(message.Market, message.Granularity, message.Interval);

                foreach (var strategy in strategies)
                {
                    if (strategy.Active)
                    {
                        var levels = _priceLevelStrategy.Calculate(candles, 7);
                        _client.AddPriceLevel(levels);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to Update Market Metrics message {e.Message} {e.StackTrace}");
            }
        }
    }
}
using Archimedes.Library.Message;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Strategy.Http;
using Archimedes.Service.Strategy.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public class StrategySubscriber : IStrategySubscriber
    {
        private readonly ILogger<StrategySubscriber> _logger;
        private readonly IStrategyConsumer _consumer;
        private readonly ICandleLoader _loader;
        private readonly IPriceLevelStrategy _priceLevelStrategy;
        private readonly IHttpRepositoryClient _client;
        private readonly IHubContext<StrategyHub> _context;
        private readonly IProducerFanout<List<PriceLevelDto>> _producerFanout;

        public StrategySubscriber(ILogger<StrategySubscriber> logger, IStrategyConsumer consumer, ICandleLoader loader,
            IPriceLevelStrategy priceLevelStrategy, IHttpRepositoryClient client, IHubContext<StrategyHub> context, IProducerFanout<List<PriceLevelDto>> producerFanout)
        {
            _logger = logger;
            _consumer = consumer;
            _loader = loader;
            _priceLevelStrategy = priceLevelStrategy;
            _client = client;
            _context = context;
            _producerFanout = producerFanout;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        public void Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(cancellationToken);
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            var message = JsonConvert.DeserializeObject<StrategyMessage>(args.Message);

            _logger.LogInformation($"Received from StrategyResponseQueue: {message}");
            RunStrategies(message);
        }

        private async void RunStrategies(StrategyMessage message)
        {
            try
            {
                var marketCandles =  await _client.GetCandlesByGranularityMarket(message.Market, message.Granularity);
                var strategies = await _client.GetStrategiesByGranularityMarket(message.Market, message.Granularity);
                var candles = _loader.Load(marketCandles);

                foreach (var strategy in strategies)
                {
                    if (strategy.Active)
                    {
                        var levels =
                            _priceLevelStrategy.Calculate(
                                candles.Where(a => a.TimeStamp >= strategy.EndDate)
                                    .ToList(), 7);

                        if (levels == null) continue;
                        {
                            if (!levels.Any()) continue;

                            _producerFanout.PublishMessage(levels,"Archimedes_Price_Level");
                            //push to fanout
                            _client.AddPriceLevel(levels);

                            strategy.EndDate = levels.Max(a => a.TimeStamp);
                            //strategy.StartDate = levels.Min(a => a.TimeStamp);
                            strategy.Count = levels.Count;
                            strategy.LastUpdated = DateTime.Now;

                            _client.UpdateStrategyMetrics(strategy);
                            await _context.Clients.All.SendAsync("Update", strategy);
                        }
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
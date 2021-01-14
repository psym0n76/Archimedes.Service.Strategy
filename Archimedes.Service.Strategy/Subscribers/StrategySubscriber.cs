using Archimedes.Library.Message;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Strategy.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Logger;

namespace Archimedes.Service.Strategy
{
    public class StrategySubscriber : IStrategySubscriber
    {
        private readonly ILogger<StrategySubscriber> _logger;
        private readonly IStrategyConsumer _consumer;
        private readonly ICandleHistoryLoader _loader;
        private readonly IPriceLevelStrategy _priceLevelStrategy;
        private readonly IHttpRepositoryClient _client;
        private readonly IStrategyPublisher _publisher;

        private readonly BatchLog _batchLog = new();
        private string _logId;

        public StrategySubscriber(ILogger<StrategySubscriber> logger, IStrategyConsumer consumer,
            ICandleHistoryLoader loader,
            IPriceLevelStrategy priceLevelStrategy, IHttpRepositoryClient client, IStrategyPublisher publisher)
        {
            _logger = logger;
            _consumer = consumer;
            _loader = loader;
            _priceLevelStrategy = priceLevelStrategy;
            _client = client;
            _publisher = publisher;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        public void Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(cancellationToken, 1000);
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            _logId = _batchLog.Start(nameof(Consumer_HandleMessage));
            var message = JsonConvert.DeserializeObject<StrategyMessage>(args.Message);

            _batchLog.Update(_logId, "Request from StrategyResponseQueue");
            RunStrategies(message);
        }

        private async void RunStrategies(StrategyMessage message)
        {
            try
            {
                var strategies = await _client.GetStrategiesByGranularityMarket(message.Market, message.Granularity);

                if (!strategies.Any())
                {
                    _logger.LogInformation(_batchLog.Print(_logId, $"Strategies missing for {message.Market} {message.Granularity}"));
                    return;
                }

                _batchLog.Update(_logId,
                    $"{strategies.Count} Strategies returned from Table for {message.Market} {message.Granularity}");

                foreach (var strategy in strategies)
                {
                    _batchLog.Update(_logId, $"Running Strategy {strategy.Name}");

                    var candles = await LoadCandles(message, strategy.EndDate);

                    if (!candles.Any())
                    {
                        _batchLog.Update(_logId, $"Candle(s) missing {candles.Count}");
                        continue;
                    }

                    _batchLog.Update(_logId, $"{candles.Count} Candle(s) returned");

                    var levels = _priceLevelStrategy.Calculate(candles, 7);

                    if (!levels.Any())
                    {
                        _batchLog.Update(_logId, $"PriceLevel(s) missing {levels.Count}");
                        continue;
                    }

                    _batchLog.Update(_logId, $"{levels.Count} PriceLevel(s) returned");

                    foreach (var level in levels)
                    {
                        await _publisher.AddTableAndPublishToQueue(level, strategy);
                    }
                }

                _logger.LogInformation(_batchLog.Print(_logId));
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"Error returned from StrategySubscriber",e));
            }
        }


        private async Task<List<Candle>> LoadCandles(StrategyMessage message, DateTime endDate)
        {
            var marketCandles = await _client.GetCandlesByGranularityMarketByDate(message.Market, message.Granularity,
                endDate.AddDays(-1), DateTime.Now);

            if (!marketCandles.Any())
            {
                _batchLog.Update(_logId, $"No Candles in CandleLoader");
                return new List<Candle>();
            }

            _batchLog.Update(_logId,
                $"Loaded {marketCandles.Count} Candles");

            var candles = _loader.Load(marketCandles);
            _batchLog.Update(_logId, $"Loaded {candles.Count} Candles in CandleLoader");
            return candles;
        }
    }
}
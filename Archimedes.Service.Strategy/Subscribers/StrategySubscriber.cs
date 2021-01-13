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
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public class StrategySubscriber : IStrategySubscriber
    {
        private readonly ILogger<StrategySubscriber> _logger;
        private readonly IStrategyConsumer _consumer;
        private readonly ICandleHistoryLoader _loader;
        private readonly IPriceLevelStrategy _priceLevelStrategy;
        private readonly IHttpRepositoryClient _client;
        private readonly IHubContext<StrategyHub> _context;
        private readonly IProducerFanout<PriceLevelMessage> _producerFanout;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public StrategySubscriber(ILogger<StrategySubscriber> logger, IStrategyConsumer consumer,
            ICandleHistoryLoader loader,
            IPriceLevelStrategy priceLevelStrategy, IHttpRepositoryClient client, IHubContext<StrategyHub> context,
            IProducerFanout<PriceLevelMessage> producerFanout)
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
            _consumer.Subscribe(cancellationToken, 1000);
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            _logId = _batchLog.Start();
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
                        if (await AddTableAndPublishToQueue(level, strategy)) break;
                    }
                }

                _logger.LogInformation(_batchLog.Print(_logId));
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"Error returned from StrategySubscriber",e));
            }
        }

        private async Task<bool> AddTableAndPublishToQueue(PriceLevelDto level, StrategyDto strategy)
        {
            _batchLog.Update(_logId, $"ADD PriceLevel {level.Market} {level.BuySell} {level.TimeStamp} to Table");

            var levelDto = await _client.AddPriceLevel(level);

            if (levelDto.Strategy == "Duplicate")
            {
                _batchLog.Update(_logId, $"NOT ADDED Duplication");
                return true;
            }

            _batchLog.Update(_logId, $"ADDED Id={levelDto.Id} to Table");

            PublishToQueue(strategy, levelDto);

            await UpdateStrategyMetrics(strategy, level);
            return false;
        }

        private void PublishToQueue(StrategyDto strategy, PriceLevelDto level)
        {
            var levelMessage = new PriceLevelMessage
            {
                Market = strategy.Market,
                Strategy = strategy.Name,
                Granularity = strategy.Granularity,
                PriceLevels = new List<PriceLevelDto>() {level}
            };

            _batchLog.Update(_logId, $"Publish to ArchimedesPriceLevels");
            _producerFanout.PublishMessage(levelMessage, "Archimedes_Price_Level");
        }

        private async Task UpdateStrategyMetrics(StrategyDto strategy, PriceLevelDto level)
        {
            strategy.EndDate = level.TimeStamp;
            strategy.LastUpdated = DateTime.Now;

            _batchLog.Update(_logId, $"Update StrategyMetrics to Table");
            _client.UpdateStrategyMetrics(strategy);

            _batchLog.Update(_logId, $"Publish StrategyMetrics to Hub");
            await _context.Clients.All.SendAsync("Update", strategy);
        }

        private async Task<List<Candle>> LoadCandles(StrategyMessage message, DateTime endDate)
        {
            var marketCandles = await _client.GetCandlesByGranularityMarketByDate(message.Market, message.Granularity,
                endDate.AddDays(-1), DateTime.Now);

            if (marketCandles == null)
            {
                _batchLog.Update(_logId, $"No Candles in CandleLoader");
                return null;
            }

            if (!marketCandles.Any())
            {
                _batchLog.Update(_logId, $"No Candles in CandleLoader");
                return null;
            }

            _batchLog.Update(_logId,
                $"Loaded {marketCandles.Count} Candles");

            var candles = _loader.Load(marketCandles);
            _batchLog.Update(_logId, $"Loaded {candles.Count} Candles in CandleLoader");
            return candles;
        }
    }
}
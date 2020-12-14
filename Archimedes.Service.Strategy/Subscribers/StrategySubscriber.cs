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
        private readonly ICandleLoader _loader;
        private readonly IPriceLevelStrategy _priceLevelStrategy;
        private readonly IHttpRepositoryClient _client;
        private readonly IHubContext<StrategyHub> _context;
        private readonly IProducerFanout<PriceLevelDto> _producerFanout;
        private readonly BatchLog _batchLog = new BatchLog();
        private string _logId;

        public StrategySubscriber(ILogger<StrategySubscriber> logger, IStrategyConsumer consumer, ICandleLoader loader,
            IPriceLevelStrategy priceLevelStrategy, IHttpRepositoryClient client, IHubContext<StrategyHub> context,
            IProducerFanout<PriceLevelDto> producerFanout)
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
                _batchLog.Update(_logId,
                    $"Loaded {strategies.Count} strategies against {message.Market} {message.Granularity}");

                if (!strategies.Any())
                {
                    _batchLog.Update(_logId, "No Active strategies found");
                    _logger.LogInformation(_batchLog.Print(_logId));
                    return;
                }

                foreach (var strategy in strategies)
                {
                    var candles = await LoadCandles(message, strategy.EndDate);

                    if (candles == null)
                    {
                        continue;
                    }

                    _batchLog.Update(_logId, $"Starting strategy {strategy.Name}");

                    //var levels =
                    //    _priceLevelStrategy.Calculate(
                    //        candles.Where(a => a.TimeStamp >= strategy.EndDate).ToList(), 7);

                    var levels = _priceLevelStrategy.Calculate(candles, 7);

                    _batchLog.Update(_logId, $"PriceLevels response {levels.Count}");

                    if (!levels.Any())
                    {
                        continue;
                    }

                    {
                        _batchLog.Update(_logId, $"Publish to ArchimedesPriceLevels");
                        _producerFanout.PublishMessages(levels, "Archimedes_Price_Level");

                        _batchLog.Update(_logId, $"Post PriceLevels to Repo API");
                        _client.AddPriceLevel(levels);

                        strategy.EndDate = levels.Max(a => a.TimeStamp);
                        //strategy.StartDate = levels.Min(a => a.TimeStamp);
                        strategy.Count = levels.Count;
                        strategy.LastUpdated = DateTime.Now;

                        _batchLog.Update(_logId, $"Update StrategyMetrics to Repo API");
                        _client.UpdateStrategyMetrics(strategy);

                        _batchLog.Update(_logId, $"Publish StrategyMetrics to Hub");
                        await _context.Clients.All.SendAsync("Update", strategy);
                        _batchLog.Update(_logId, $"Ending strategy {strategy.Name}");
                    }

                }

                _logger.LogInformation(_batchLog.Print(_logId));
            }
            catch (Exception e)
            {
                _batchLog.Update(_logId, $"Unable to run Strategies  {e.Message} {e.StackTrace}");
                _logger.LogError(_batchLog.Print(_logId));
            }
        }

        private async Task<List<Candle>> LoadCandles(StrategyMessage message, DateTime endDate)
        {
            //var marketCandles = await _client.GetCandlesByGranularityMarket(message.Market, message.Granularity);
            var marketCandles = await _client.GetCandlesByGranularityMarketByDate(message.Market, message.Granularity,
                endDate, DateTime.Now);

            if (!marketCandles.Any())
            {
                _batchLog.Update(_logId, $"No Candles in CandleLoader");
                return null;
            }

            _batchLog.Update(_logId,
                $"Loaded {marketCandles.Count} MarketCandles");

            var candles = _loader.Load(marketCandles);
            _batchLog.Update(_logId, $"Loaded {candles.Count} Candles in CandleLoader");
            return candles;
        }
    }
}
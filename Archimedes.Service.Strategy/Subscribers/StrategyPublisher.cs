using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Strategy.Http;
using Archimedes.Service.Strategy.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class StrategyPublisher : IStrategyPublisher
    {
        private readonly BatchLog _batchLog = new();
        private string _logId;
        private readonly IHttpRepositoryClient _client;
        private readonly IHubContext<StrategyHub> _strategyHub;
        private readonly IHubContext<PriceLevelHub> _priceLevelHub;
        private readonly IProducerFanout<PriceLevelMessage> _producerFanout;
        private readonly ILogger<StrategyPublisher> _logger;

        public StrategyPublisher(IHttpRepositoryClient client, IHubContext<StrategyHub> strategyHub,
            IHubContext<PriceLevelHub> priceLevelHub, IProducerFanout<PriceLevelMessage> producerFanout, ILogger<StrategyPublisher> logger)
        {
            _client = client;
            _strategyHub = strategyHub;
            _priceLevelHub = priceLevelHub;
            _producerFanout = producerFanout;
            _logger = logger;
        }

        public async Task AddTableAndPublishToQueue(PriceLevelDto level, StrategyDto strategy)
        {
            _logId = _batchLog.Start($"{nameof(AddTableAndPublishToQueue)}");

            try
            {
                if (!await PublishToTable(level)) return;

                PublishToQueue(strategy, level);
                PublishToHub(level);

                await UpdateStrategyMetrics(strategy, level);
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId), e);
            }

            _logger.LogInformation(_batchLog.Print(_logId));
        }

        private async Task<bool> PublishToTable(PriceLevelDto level)
        {
            _batchLog.Update(_logId, $"ADD PriceLevel {level.Market} {level.BuySell} {level.TimeStamp} to Table");

            var levelDto = await _client.AddPriceLevel(level);

            if (levelDto.Strategy == "Duplicate")
            {
                _batchLog.Update(_logId, $"NOT ADDED Duplication");
                return false;
            }

            level.Id = levelDto.Id;
            _batchLog.Update(_logId, $"ADDED Id={level.Id} to Table");
            return true;
        }

        private void PublishToHub(PriceLevelDto level)
        {
            _batchLog.Update(_logId, $"Publish PriceLevel to Hub {level.Granularity} {level.TimeStamp}");
            _priceLevelHub.Clients.All.SendAsync("Update", level);
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
            await _strategyHub.Clients.All.SendAsync("Update", strategy);
        }
    }
}
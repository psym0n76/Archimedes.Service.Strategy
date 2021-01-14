using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PriceLevelStrategy : IPriceLevelStrategy
    {
        private readonly ILogger<PriceLevelStrategy> _logger;
        private readonly IPivotLevelStrategyHigh _levelStrategyHigh;
        private readonly IPivotLevelStrategyLow _levelStrategyLow;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public PriceLevelStrategy(ILogger<PriceLevelStrategy> logger, IPivotLevelStrategyHigh levelStrategyHigh,
            IPivotLevelStrategyLow levelStrategyLow)
        {
            _logger = logger;
            _levelStrategyHigh = levelStrategyHigh;
            _levelStrategyLow = levelStrategyLow;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {
            _logId = _batchLog.Start("Pivot Level Strategy");
            
            if (!candles.Any())
            {
                _batchLog.Print(_logId, "Candles empty");
                return new List<PriceLevelDto>();
            }

            var market = candles.First().Market;
            var timeFrame = candles.First().TimeFrame;

            _batchLog.Update(_logId, $"STARTED: Market: {market} TimeFrame: {timeFrame} Candles: {candles.Count} with Pivot: {pivotCount}");

            var candleLevelsBag = new ConcurrentBag<PriceLevelDto>();

            var taskPivotHigh = Task.Run(() =>
            {
                candleLevelsBag.AddRange(_levelStrategyHigh.Calculate(candles, pivotCount));
            });

            var taskPivotLow = Task.Run(() =>
            {
                candleLevelsBag.AddRange(_levelStrategyLow.Calculate(candles, pivotCount));
            });

            Task.WaitAll(taskPivotHigh, taskPivotLow);
            
            _logger.LogInformation(_batchLog.Print(_logId, $"ENDED: Market: {market} TimeFrame: {timeFrame}"));

            var orderedList = candleLevelsBag.OrderBy(a => a.TimeStamp);

            return orderedList.ToList();
        }
    }
}
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PriceLevelStrategy : IPriceLevelStrategy
    {
        private readonly ILogger<PriceLevelStrategy> _logger;
        private readonly IPivotLevelStrategyHigh _levelStrategyHigh;
        private readonly IPivotLevelStrategyLow _levelStrategyLow;

        public PriceLevelStrategy(ILogger<PriceLevelStrategy> logger, IPivotLevelStrategyHigh levelStrategyHigh,
            IPivotLevelStrategyLow levelStrategyLow)
        {
            _logger = logger;
            _levelStrategyHigh = levelStrategyHigh;
            _levelStrategyLow = levelStrategyLow;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {
            if (candles == null)
            {
                _logger.LogError("PriceLevelStrategy: Candle Collection empty");
                return default;
            }

            var market = candles.First().Market;
            var timeFrame = candles.First().TimeFrame;

            _logger.LogInformation($"PriceLevelStrategy STARTED: Market: {market} TimeFrame: {timeFrame} Candles: {candles.Count} with Pivot: {pivotCount}");

            var timer = new Stopwatch();
            timer.Start();

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

            _logger.LogInformation(
                $"PriceLevelStrategy ENDED: Market: {market} TimeFrame: {timeFrame} Candles: {candles.Count} with Pivot: {pivotCount} in {timer.Elapsed.Seconds}secs");

            return candleLevelsBag.OrderBy(a => a.TimeStamp).ToList();
        }
    }
}


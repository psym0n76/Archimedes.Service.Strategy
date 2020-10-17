using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PriceLevelStrategy
    {
        private readonly ILogger<PriceLevelStrategy> _logger;

        public PriceLevelStrategy(ILogger<PriceLevelStrategy> logger)
        {
            _logger = logger;
        }

        public List<Candle> Calculate(List<Candle> candles, int pivotCount)
        {
            var pivotLevels = new List<Candle>();

            var taskPivotHigh = Task.Run(() => { pivotLevels.AddRange(CalculatePivotHigh(candles, pivotCount)); });
            var taskPivotLow = Task.Run(() => { pivotLevels.AddRange(CalculatePivotLow(candles, pivotCount)); });

            Task.WaitAll(taskPivotHigh, taskPivotLow);

            return pivotLevels.OrderBy(a => a.TimeStamp).ToList();
        }

        public List<Candle> CalculatePivotLow(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<Candle>();

            foreach (var candle in candles)
            {
                var pastPivotLow = PivotLow(candle, candle.PastCandles.Take(pivotCount));
                var futurePivotLow = PivotLow(candle, candle.FutureCandles.Take(pivotCount));

                if (!pastPivotLow || !futurePivotLow) continue;
                _logger.LogInformation($"PivotHigh Low found: {candle}");
                priceLevels.Add(candle);
            }

            return priceLevels;
        }

        public List<Candle> CalculatePivotHigh(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<Candle>();

            foreach (var candle in candles)
            {
                var pastPivotHigh = PivotHigh(candle, candle.PastCandles.Take(pivotCount));
                var futurePivotHigh = PivotHigh(candle, candle.FutureCandles.Take(pivotCount));

                if (pastPivotHigh && futurePivotHigh)
                {
                    _logger.LogInformation($"PivotHigh High found: {candle}");
                    priceLevels.Add(candle);
                }
            }

            return priceLevels;
        }

        private static bool PivotHigh(Candle candle, IEnumerable<Candle> history)
        {
            var pivot = false;
            foreach (var candleHistoryCandle in history)
            {
                if (candle.High.Bid >= candleHistoryCandle.High.Bid)
                {
                    pivot = true;
                }
                else
                {
                    pivot = false;
                    break;
                }
            }

            return pivot;
        }

        private static bool PivotLow(Candle candle, IEnumerable<Candle> history)
        {
            var pivot = false;
            foreach (var candleHistoryCandle in history)
            {
                if (candle.Low.Bid <= candleHistoryCandle.Low.Bid)
                {
                    pivot = true;
                }
                else
                {
                    pivot = false;
                    break;
                }
            }

            return pivot;
        }
    }
}
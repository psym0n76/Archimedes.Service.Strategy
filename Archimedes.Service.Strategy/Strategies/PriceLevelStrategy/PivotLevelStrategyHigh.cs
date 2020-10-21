using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PivotLevelStrategyHigh : IPivotLevelStrategyHigh
    {
        private readonly ILogger<PivotLevelStrategyHigh> _logger;

        public PivotLevelStrategyHigh(ILogger<PivotLevelStrategyHigh> logger)
        {
            _logger = logger;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<PriceLevelDto>();

            foreach (var candle in candles)
            {
                var pastPivotHigh = PivotHigh(candle, candle.PastCandles, pivotCount);
                var futurePivotHigh = PivotHigh(candle, candle.FutureCandles, pivotCount);

                if (!pastPivotHigh || !futurePivotHigh) continue;

                _logger.LogInformation($"PivotHigh found: {candle}");

                var p = new PriceLevelDto()
                {
                    TimeStamp = candle.TimeStamp,
                    Granularity = candle.TimeFrame,
                    Market = candle.Market,

                    AskPrice = double.Parse(candle.Top().Ask.ToString(CultureInfo.InvariantCulture)),
                    AskPriceRange = double.Parse(candle.High.Ask.ToString(CultureInfo.InvariantCulture)),

                    BidPrice = double.Parse(candle.Top().Bid.ToString(CultureInfo.InvariantCulture)),
                    BidPriceRange = double.Parse(candle.High.Bid.ToString(CultureInfo.InvariantCulture)),

                    Strategy = "PIVOT HIGH " + pivotCount,
                    TradeType = "SELL",
                    CandleType = candle.BodyFillRate().ToString(CultureInfo.InvariantCulture),
                    LastUpdated = DateTime.Now
                };

                priceLevels.Add(p);
            }

            return priceLevels;
        }

        private static bool PivotHigh(Candle candle, IEnumerable<Candle> history, int pivotCount)
        {
            var pivot = false;

            //ensure we dont use the first or last candles
            var candles = history.Take(pivotCount).ToList();

            if (candles.Count() < pivotCount)
            {
                return false;
            }

            foreach (var candleHistoryCandle in candles)
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
    }
}
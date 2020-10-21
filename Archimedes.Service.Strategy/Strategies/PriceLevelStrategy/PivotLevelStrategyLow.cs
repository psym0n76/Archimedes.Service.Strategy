using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PivotLevelStrategyLow : IPivotLevelStrategyLow
    {
        private readonly ILogger<PivotLevelStrategyLow> _logger;

        public PivotLevelStrategyLow(ILogger<PivotLevelStrategyLow> logger)
        {
            _logger = logger;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<PriceLevelDto>();

            foreach (var candle in candles)
            {
                var pastPivotLow = PivotLow(candle, candle.PastCandles, pivotCount);
                var futurePivotLow = PivotLow(candle, candle.FutureCandles, pivotCount);

                if (!pastPivotLow || !futurePivotLow) continue;

                _logger.LogInformation($"PivotLow found: {candle}");

                var p = new PriceLevelDto()
                {
                    TimeStamp = candle.TimeStamp,
                    Granularity = candle.TimeFrame,
                    Market = candle.Market,
                    Active = "True",

                    AskPrice = double.Parse(candle.Bottom().Ask.ToString(CultureInfo.InvariantCulture)),
                    AskPriceRange = double.Parse(candle.Low.Ask.ToString(CultureInfo.InvariantCulture)),

                    BidPrice = double.Parse(candle.Bottom().Bid.ToString(CultureInfo.InvariantCulture)),
                    BidPriceRange = double.Parse(candle.Low.Bid.ToString(CultureInfo.InvariantCulture)),

                    Strategy = "PIVOT LOW " + pivotCount,
                    TradeType = "BUY",
                    CandleType = candle.BodyFillRate().ToString(CultureInfo.InvariantCulture),
                    LastUpdated = DateTime.Now,

                };
                priceLevels.Add(p);
            }
            return priceLevels;
        }

        private static bool PivotLow(Candle candle, IEnumerable<Candle> history, int pivotCount)
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Archimedes.Library.Candles;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PivotLevelStrategyLow : IPivotLevelStrategyLow
    {
        private readonly ILogger<PivotLevelStrategyLow> _logger;
        private readonly BatchLog _batchLog = new BatchLog();
        private string _logId;

        public PivotLevelStrategyLow(ILogger<PivotLevelStrategyLow> logger)
        {
            _logger = logger;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {

            _logId = _batchLog.Start();
            _batchLog.Update(_logId, "Start Pivot Level Low");
            
            var priceLevels = new List<PriceLevelDto>();

            foreach (var candle in candles)
            {
                var pastPivotLow = PivotLow(candle, candle.PastCandles, pivotCount);
                var futurePivotLow = PivotLow(candle, candle.FutureCandles, pivotCount);

                if (!pastPivotLow || !futurePivotLow) continue;

                _batchLog.Update(_logId, $"PivotLow found: {candle.TimeStamp} {candle.TimeFrame}");

                var p = new PriceLevelDto()
                {
                    TimeStamp = candle.TimeStamp,
                    Granularity = candle.TimeFrame,
                    Market = candle.Market,
                    Active = true,

                    AskPrice = candle.Bottom().Ask,
                    AskPriceRange = candle.Low.Ask,

                    BidPrice = candle.Bottom().Bid,
                    BidPriceRange = candle.Low.Bid,

                    Strategy = "PIVOT LOW " + pivotCount,
                    BuySell = "BUY",
                    CandleType = candle.BodyFillRate().ToString(CultureInfo.InvariantCulture),
                    LastUpdated = DateTime.Now,

                };
                priceLevels.Add(p);
            }
            _logger.LogInformation(_batchLog.Print(_logId));

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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class PriceLevelStrategy : IPriceLevelStrategy
    {
        private readonly ILogger<PriceLevelStrategy> _logger;

        public PriceLevelStrategy(ILogger<PriceLevelStrategy> logger)
        {
            _logger = logger;
        }

        public List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount)
        {
            var candleLevels = new List<PriceLevelDto>();

            var taskPivotHigh = Task.Run(() => { candleLevels.AddRange(CalculatePivotHigh(candles, pivotCount)); });
            var taskPivotLow = Task.Run(() => { candleLevels.AddRange(CalculatePivotLow(candles, pivotCount)); });

            Task.WaitAll(taskPivotHigh, taskPivotLow);

            return candleLevels.OrderBy(a => a.TimeStamp).ToList();
        }

        public List<PriceLevelDto> CalculatePivotLow(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<PriceLevelDto>();

            foreach (var candle in candles)
            {
                var pastPivotLow = PivotLow(candle, candle.PastCandles.Take(pivotCount));
                var futurePivotLow = PivotLow(candle, candle.FutureCandles.Take(pivotCount));

                if (!pastPivotLow || !futurePivotLow) continue;
                _logger.LogInformation($"PivotLow found: {candle}");

                var p = new PriceLevelDto()
                {
                    TimeStamp = candle.TimeStamp,
                    Granularity = candle.TimeFrame,
                    Market = candle.Market,
                    Active = "True",

                    AskPrice = double.Parse(candle.High.Ask.ToString(CultureInfo.InvariantCulture)),
                    AskPriceRange = double.Parse(candle.Top().Ask.ToString(CultureInfo.InvariantCulture)),

                    BidPrice = double.Parse(candle.High.Bid.ToString(CultureInfo.InvariantCulture)),
                    BidPriceRange = double.Parse(candle.Top().Bid.ToString(CultureInfo.InvariantCulture)),

                    Strategy = "PIVOT LOW " + pivotCount,
                    TradeType = "BUY",
                    CandleType = "convertenumtostring",
                    LastUpdated = DateTime.Now,

                };
                priceLevels.Add(p);
            }

            return priceLevels;
        }

        public List<PriceLevelDto> CalculatePivotHigh(List<Candle> candles, int pivotCount)
        {
            var priceLevels = new List<PriceLevelDto>();

            foreach (var candle in candles)
            {
                var pastPivotHigh = PivotHigh(candle, candle.PastCandles.Take(pivotCount));
                var futurePivotHigh = PivotHigh(candle, candle.FutureCandles.Take(pivotCount));

                if (pastPivotHigh && futurePivotHigh)
                {
                    _logger.LogInformation($"PivotHigh found: {candle}");

                    var p = new PriceLevelDto()
                    {
                        TimeStamp = candle.TimeStamp,
                        Granularity = candle.TimeFrame,
                        Market = candle.Market,

                        AskPrice = double.Parse(candle.High.Ask.ToString(CultureInfo.InvariantCulture)),
                        AskPriceRange = double.Parse(candle.Bottom().Ask.ToString(CultureInfo.InvariantCulture)),

                        BidPrice = double.Parse(candle.High.Bid.ToString(CultureInfo.InvariantCulture)),
                        BidPriceRange = double.Parse(candle.Bottom().Bid.ToString(CultureInfo.InvariantCulture)),

                        Strategy = "PIVOT HIGH " + pivotCount,
                        TradeType = "SELL",
                        CandleType = "convertenumtostring",
                        LastUpdated = DateTime.Now
                    };

                    priceLevels.Add(p);
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
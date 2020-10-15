using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Ui.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Strategy
{
    public class CandleLoader : ICandleLoader
    {
        private readonly IHttpRepositoryClient _client;
        private readonly ILogger<CandleLoader> _logger;

        public CandleLoader(IHttpRepositoryClient client, ILogger<CandleLoader> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<List<Candle>> Load(string market, string granularity, int interval)
        {
            var candlesByGranularityMarket = await _client.GetCandlesByGranularityMarket(market, granularity);

            var candles = new List<Candle>();
            var elapsedTime = new Stopwatch();
            elapsedTime.Start();

            foreach (var currentCandle in candlesByGranularityMarket)
            {
                var candle = LoadCandle(currentCandle);

                var taskFutureCandles = Task.Run(() =>
                {
                    candle.FutureCandles.AddRange(GetCandles(candlesByGranularityMarket, currentCandle, interval));
                });

                var taskPastCandles = Task.Run(() =>
                {
                    candle.PastCandles.AddRange(GetCandles(candlesByGranularityMarket, currentCandle, -interval)
                        .OrderByDescending(a => a.TimeStamp));
                });

                Task.WaitAll(taskPastCandles, taskFutureCandles);

                candles.Add(candle);
            }

            _logger.LogInformation(
                $"Candles loaded: {candlesByGranularityMarket.Count} in {elapsedTime.Elapsed.TotalSeconds} secs");
            return candles;
        }

        private static Candle LoadCandle(CandleDto dto)
        {
            var candle = new Candle(
                new Open(dto.BidOpen.ToDecimal(), dto.AskOpen.ToDecimal()),
                new High(dto.BidHigh.ToDecimal(), dto.AskHigh.ToDecimal()),
                new Low(dto.BidLow.ToDecimal(), dto.AskLow.ToDecimal()),
                new Close(dto.BidClose.ToDecimal(), dto.AskClose.ToDecimal()),
                dto.Market, dto.Granularity, dto.ToDate);
            return candle;
        }

        private static IEnumerable<Candle> GetCandles(IEnumerable<CandleDto> candleHistory, CandleDto currentCandle,
            int interval)
        {
            List<CandleDto> historyCandles;

            if (interval > 0)
            {
                historyCandles = candleHistory.Where(a =>
                    a.FromDate > currentCandle.FromDate &&
                    a.FromDate <= currentCandle.FromDate.AddMinutes(interval * 15)).ToList();
            }
            else
            {
                historyCandles = candleHistory.Where(a =>
                    a.FromDate > currentCandle.FromDate.AddMinutes(interval * 15) &&
                    a.FromDate < currentCandle.FromDate).ToList();
            }


            return historyCandles.Select(hist =>
                new Candle(
                    new Open(hist.BidOpen.ToDecimal(), hist.AskOpen.ToDecimal()),
                    new High(hist.BidHigh.ToDecimal(), hist.AskHigh.ToDecimal()),
                    new Low(hist.BidLow.ToDecimal(), hist.AskLow.ToDecimal()),
                    new Close(hist.BidClose.ToDecimal(), hist.AskClose.ToDecimal()), hist.Market, hist.Granularity,
                    hist.ToDate)).ToList();
        }
    }
}
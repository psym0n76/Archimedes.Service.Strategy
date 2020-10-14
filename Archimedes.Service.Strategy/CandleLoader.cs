using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Enums;
using Archimedes.Library.Extensions;
using Archimedes.Service.Ui.Http;
using Microsoft.VisualBasic;

namespace Archimedes.Service.Strategy
{
    public class CandleLoader : ICandleLoader
    {
        private readonly IHttpRepositoryClient _client;

        public CandleLoader(IHttpRepositoryClient client)
        {
            _client = client;
        }

        public async Task<List<Candle>> Load(string market, string granularity, int interval)
        {
            var candleDto = await _client.GetCandlesByGranularityMarket(market, granularity);

            var candles = new List<Candle>();

            foreach (var dto in candleDto)
            {
                var candle = new Candle(
                    new Open(dto.BidOpen.ToDecimal(), dto.AskOpen.ToDecimal()),
                    new High(dto.BidHigh.ToDecimal(), dto.AskHigh.ToDecimal()),
                    new Low(dto.BidLow.ToDecimal(), dto.AskLow.ToDecimal()),
                    new Close(dto.BidClose.ToDecimal(), dto.AskClose.ToDecimal()),
                    dto.Market, dto.Granularity, dto.ToDate);

                var historyCandles = candleDto.Where(a =>
                    a.ToDate <= dto.ToDate.AddMinutes(interval * 15) && a.FromDate >= dto.FromDate.AddMinutes(-interval * 15));


                candle.HistoryCandles = new List<Candle>();

                foreach (var hist in historyCandles)
                {
                    var history = new Candle(
                        new Open(hist.BidOpen.ToDecimal(), hist.AskOpen.ToDecimal()),
                        new High(hist.BidHigh.ToDecimal(), hist.AskHigh.ToDecimal()),
                        new Low(hist.BidLow.ToDecimal(), hist.AskLow.ToDecimal()),
                        new Close(hist.BidClose.ToDecimal(), hist.AskClose.ToDecimal()),
                        hist.Market, hist.Granularity, hist.ToDate);

                    candle.HistoryCandles.Add(history);
                }

                candles.Add(candle);
            }

            return candles;
        }
    }
}
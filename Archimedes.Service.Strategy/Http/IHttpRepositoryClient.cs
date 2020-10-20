using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy.Http
{
    public interface IHttpRepositoryClient
    {
        Task<List<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity);
        Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate);
        void AddPriceLevel(List<PriceLevelDto> priceLevel);

        Task<List<StrategyDto>> GetStrategiesByGranularityMarket(string market, string granularity);
    }
}
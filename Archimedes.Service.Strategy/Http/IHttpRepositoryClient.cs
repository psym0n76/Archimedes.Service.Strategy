using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Microsoft.VisualBasic;

namespace Archimedes.Service.Ui.Http
{
    public interface IHttpRepositoryClient
    {
        Task<List<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity);
        Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate);
        void AddPriceLevel(List<PriceLevelDto> priceLevel);
    }
}
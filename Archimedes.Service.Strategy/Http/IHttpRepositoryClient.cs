using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Microsoft.VisualBasic;

namespace Archimedes.Service.Ui.Http
{
    public interface IHttpRepositoryClient
    {
        Task<IEnumerable<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity);
        Task<IEnumerable<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate);
    }
}
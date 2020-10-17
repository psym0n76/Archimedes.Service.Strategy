using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Candles;

namespace Archimedes.Service.Strategy
{
    public interface ICandleLoader
    {
        Task<List<Candle>> Load(string market, string granularity, int interval);
    }
}
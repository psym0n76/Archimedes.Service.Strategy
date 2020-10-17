using System.Collections.Generic;
using Archimedes.Library.Candles;

namespace Archimedes.Service.Strategy
{
    public interface IPriceLevelStrategy
    {
        List<Candle> Calculate(List<Candle> candles, int pivotCount);
        List<Candle> CalculatePivotLow(List<Candle> candles, int pivotCount);
        List<Candle> CalculatePivotHigh(List<Candle> candles, int pivotCount);
    }
}
using System.Collections.Generic;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public interface IPriceLevelStrategy
    {
        List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount);
        List<PriceLevelDto> CalculatePivotLow(List<Candle> candles, int pivotCount);
        List<PriceLevelDto> CalculatePivotHigh(List<Candle> candles, int pivotCount);
    }
}
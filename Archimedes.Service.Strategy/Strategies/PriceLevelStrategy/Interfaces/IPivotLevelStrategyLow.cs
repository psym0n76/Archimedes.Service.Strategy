﻿using System.Collections.Generic;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public interface IPivotLevelStrategyLow
    {
        List<PriceLevelDto> Calculate(List<Candle> candles, int pivotCount);
    }
}
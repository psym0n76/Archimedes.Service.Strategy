using System;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public interface ITradeConsumer
    {
        event EventHandler<PriceEventArgs> HandleMessage;
        void ProcessTradeCalculations(PriceDto price);
    }
}
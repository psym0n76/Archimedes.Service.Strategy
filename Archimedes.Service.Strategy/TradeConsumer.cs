using System;
using System.Collections.Generic;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Strategy.TradeEngine;

namespace Archimedes.Service.Strategy
{

    public class TradeConsumer : ITradeConsumer
    {
        
        public event EventHandler<PriceEventArgs> HandleMessage;
        public void ProcessTradeCalculations(PriceDto price)
        {

            var priceLevels = new List<PriceLevelDto>();

            var trades = new List<Trade> {new Trade()};




            throw new NotImplementedException();
        }
    }
}
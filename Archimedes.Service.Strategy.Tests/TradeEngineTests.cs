using System.Collections.Generic;
using System.Net.NetworkInformation;
using Archimedes.Service.Strategy.TradeEngine;
using NUnit.Framework;

namespace Archimedes.Service.Strategy.Tests
{
    [TestFixture]
    public class TradeEngineTests
    {
        [Test]
        public void Should_Create_Price_Updates()
        {
            var priceStore = new PriceBank();

           // var trade1 = new Trade(priceStore);
           // var trade2 = new Trade(priceStore);
           // priceStore.AddObserver(trade1);
           // priceStore.AddObserver(trade2);

           // list of trades with price levels
            var trades = new List<Trade>()
            {
                new Trade(priceStore)
            };
            

            // subscribe each trade to a prioce update
            foreach (var trade in trades)
            {
                priceStore.AddObserver(new Trade(priceStore));
            }


            //update the price
            priceStore.Value = 12;



        }
    }
}
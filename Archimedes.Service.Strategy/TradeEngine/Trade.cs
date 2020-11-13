using System;

namespace Archimedes.Service.Strategy.TradeEngine
{
    public class Trade : IObserver
    {
        private readonly PriceBank _dataSource;

        public Trade()
        {
        }

        public Trade(PriceBank dataSource)
        {
            _dataSource = dataSource;
        }

        public void Update()
        {
            Console.WriteLine("Update received " + _dataSource.Value);
        }
    }
}
namespace Archimedes.Service.Strategy.TradeEngine
{
    public class PriceBank : PriceBankControls
    {
        private int _value;

        public int Value
        {
            get => _value;

            set
            {
                _value = value;
                NotifyObserver();
            }
        }
    }
}
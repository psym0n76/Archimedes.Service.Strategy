using System;

namespace Archimedes.Service.Strategy
{
    public class PriceEventArgs : EventArgs
    {
        public double Bid { get; set; }
        public double Ask { get; set; }
    }
}
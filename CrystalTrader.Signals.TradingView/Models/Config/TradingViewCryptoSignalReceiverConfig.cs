using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Signals.TradingView
{
    internal class TradingViewCryptoSignalReceiverConfig
    {
        public double PollingInterval { get; set; }
        public int SignalPeriod { get; set; }
        public string VolatilityPeriod { get; set; }
        public string RequestUrl { get; set; }
        public string RequestData { get; set; }
    }
}

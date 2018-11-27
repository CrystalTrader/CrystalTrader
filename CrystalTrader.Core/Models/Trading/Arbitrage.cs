using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public class Arbitrage
    {
        public bool IsAssigned { get; set; }
        public ArbitrageMarket Market { get; set; }
        public ArbitrageType Type { get; set; }
        public decimal Percentage { get; set; }
    }
}

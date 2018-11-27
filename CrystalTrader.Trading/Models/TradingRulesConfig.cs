using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Trading
{
    internal class TradingRulesConfig
    {
        public RuleProcessingMode ProcessingMode { get; set; }
        public double CheckInterval { get; set; }
    }
}

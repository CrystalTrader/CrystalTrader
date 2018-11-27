using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IBuyConfig
    {
        bool BuyEnabled { get; set; }
        OrderType BuyType { get; }
        decimal BuyMaxCost { get; }
        decimal BuyMultiplier { get; }
        decimal BuyMinBalance { get; }
        double BuySamePairTimeout { get; }
        decimal BuyTrailing { get; }
        decimal BuyTrailingStopMargin { get; }
        BuyTrailingStopAction BuyTrailingStopAction { get; }
    }
}

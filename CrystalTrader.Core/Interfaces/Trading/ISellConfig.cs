using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ISellConfig
    {
        bool SellEnabled { get; set; }
        OrderType SellType { get; }
        decimal SellMargin { get; }
        decimal SellTrailing { get; }
        decimal SellTrailingStopMargin { get; }
        SellTrailingStopAction SellTrailingStopAction { get; }
        bool SellStopLossEnabled { get; }
        bool SellStopLossAfterDCA { get; }
        double SellStopLossMinAge { get; }
        decimal SellStopLossMargin { get; }
    }
}

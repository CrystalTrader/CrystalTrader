using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ISellDCAConfig
    {
        decimal SellDCAMargin { get; }
        decimal SellDCATrailing { get; }
        decimal SellDCATrailingStopMargin { get; }
        SellTrailingStopAction SellDCATrailingStopAction { get; }
    }
}

using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Trading
{
    internal class SellTrailingInfo : TrailingInfo
    {
        public SellOptions SellOptions { get; set; }
        public SellTrailingStopAction TrailingStopAction { get; set; }
        public decimal SellMargin { get; set; }
    }
}

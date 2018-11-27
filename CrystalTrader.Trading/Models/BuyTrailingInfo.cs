using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Trading
{
    internal class BuyTrailingInfo : TrailingInfo
    {
        public BuyOptions BuyOptions { get; set; }
        public BuyTrailingStopAction TrailingStopAction { get; set; }
    }
}

﻿using CrystalTrader.Core;

namespace CrystalTrader.Exchange.Base
{
    public class Ticker : ITicker
    {
        public string Pair { get; set; }
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
        public decimal LastPrice { get; set; }
    }
}

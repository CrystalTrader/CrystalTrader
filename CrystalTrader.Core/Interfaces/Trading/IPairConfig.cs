﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IPairConfig : IBuyConfig, ISellConfig
    {
        IEnumerable<string> Rules { get; }

        int MaxPairs { get; }

        bool SwapEnabled { get; }
        List<string> SwapSignalRules { get; }
        int SwapTimeout { get; }

        bool ArbitrageEnabled { get; }
        List<ArbitrageMarket> ArbitrageMarkets { get; }
        ArbitrageType? ArbitrageType { get; }
        decimal? ArbitrageBuyMultiplier { get; }
        decimal? ArbitrageSellMultiplier { get; }
        List<string> ArbitrageSignalRules { get; }

        decimal? CurrentDCAMargin { get; }
        decimal? NextDCAMargin { get; }
    }
}

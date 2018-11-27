using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Signals.Base
{
    public class SignalRulesConfig : ISignalRulesConfig
    {
        public RuleProcessingMode ProcessingMode { get; set; }
        public double CheckInterval { get; set; }
    }
}

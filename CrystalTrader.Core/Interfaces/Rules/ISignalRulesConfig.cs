using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ISignalRulesConfig
    {
        RuleProcessingMode ProcessingMode { get; }
        double CheckInterval { get; }
    }
}

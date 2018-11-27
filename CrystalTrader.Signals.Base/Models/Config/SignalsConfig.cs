﻿using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Signals.Base
{
    public class SignalsConfig : ISignalsConfig
    {
        public bool Enabled { get; set; }
        public IEnumerable<string> GlobalRatingSignals { get; set; }
        public IEnumerable<SignalDefinition> Definitions { get; set; }

        IEnumerable<ISignalDefinition> ISignalsConfig.Definitions => Definitions;
    }
}

using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Signals.Base
{
    public interface ISignalReceiver
    {
        string SignalName { get; }
        void Start();
        void Stop();
        int GetPeriod();
        IEnumerable<ISignal> GetSignals();
        double? GetAverageRating();
    }
}

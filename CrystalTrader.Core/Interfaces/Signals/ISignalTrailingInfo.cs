using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ISignalTrailingInfo
    {
        IRule Rule { get; }
        DateTimeOffset StartTime { get; }
        double Duration { get; }
    }
}

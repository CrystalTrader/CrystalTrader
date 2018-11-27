using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface INamedService
    {
        string ServiceName { get; }
    }
}

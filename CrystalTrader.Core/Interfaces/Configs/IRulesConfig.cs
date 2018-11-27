using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IRulesConfig
    {
        IEnumerable<IModuleRules> Modules { get; }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IModuleRules
    {
        string Module { get; }
        IConfigurationSection Configuration { get; }
        IEnumerable<IRule> Entries { get; }

        T GetConfiguration<T>();
    }
}

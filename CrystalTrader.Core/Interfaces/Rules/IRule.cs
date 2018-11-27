using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IRule
    {
        bool Enabled { get; }
        string Name { get; }
        RuleAction Action { get; }
        IEnumerable<IRuleCondition> Conditions { get; }
        IRuleTrailing Trailing { get; }
        IConfigurationSection Modifiers { get; }
        T GetModifiers<T>();
    }
}

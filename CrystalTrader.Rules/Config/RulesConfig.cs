using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Rules
{
    internal class RulesConfig : IRulesConfig
    {
        public IEnumerable<ModuleRules> Modules { get; set; }
        IEnumerable<IModuleRules> IRulesConfig.Modules => Modules;
    }
}

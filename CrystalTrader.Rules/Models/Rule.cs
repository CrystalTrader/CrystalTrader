using CrystalTrader.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Rules
{
    internal class Rule : IRule
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public RuleAction Action { get; set; }
        public IEnumerable<RuleCondition> Conditions { get; set; }
        public RuleTrailing Trailing { get; set; }
        public IConfigurationSection Modifiers { get; set; }

        IEnumerable<IRuleCondition> IRule.Conditions => Conditions;
        IRuleTrailing IRule.Trailing => Trailing;

        private object typedModifiersCached;

        public T GetModifiers<T>()
        {
            if (typedModifiersCached == null)
            {
                typedModifiersCached = Modifiers.Get<T>();
            }
            return (T)typedModifiersCached;
        }
    }
}

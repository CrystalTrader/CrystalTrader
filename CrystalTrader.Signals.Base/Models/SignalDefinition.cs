using CrystalTrader.Core;
using Microsoft.Extensions.Configuration;

namespace CrystalTrader.Signals.Base
{
    public class SignalDefinition : ISignalDefinition
    {
        public string Name { get; set; }
        public string Receiver { get; set; }
        public IConfigurationSection Configuration { get; set; }
    }
}

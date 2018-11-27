using Autofac;
using CrystalTrader.Core;
using CrystalTrader.Signals.Base;

namespace CrystalTrader.Signals.TradingView
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TradingViewCryptoSignalReceiver>().As<ISignalReceiver>().Named<ISignalReceiver>(nameof(TradingViewCryptoSignalReceiver));
        }
    }
}

using Autofac;
using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Exchange.Binance
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BinanceExchangeService>().Named<IExchangeService>("Binance").As<IConfigurableService>().Named<IConfigurableService>("ExchangeBinance").SingleInstance().PreserveExistingDefaults();
        }
    }
}

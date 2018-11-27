using Autofac;
using CrystalTrader.Core;
using System;

namespace CrystalTrader.Rules
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RulesService>().As<IRulesService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.RulesService).SingleInstance();
        }
    }
}

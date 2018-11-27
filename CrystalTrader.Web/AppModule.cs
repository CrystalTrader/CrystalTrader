using Autofac;
using CrystalTrader.Core;

namespace CrystalTrader.Web
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebService>().As<IWebService>().As<IConfigurableService>().Named<IConfigurableService>(Constants.ServiceNames.WebService).SingleInstance();
        }
    }
}

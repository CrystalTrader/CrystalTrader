using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IOrderingService
    {
        IOrderDetails PlaceBuyOrder(BuyOptions options);
        IOrderDetails PlaceSellOrder(SellOptions options);
    }
}

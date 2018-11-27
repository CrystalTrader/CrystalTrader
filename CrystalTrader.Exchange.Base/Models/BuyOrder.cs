using System;
using System.Collections.Generic;
using System.Text;
using CrystalTrader.Core;

namespace CrystalTrader.Exchange.Base
{
    public class BuyOrder : Order
    {
        public override OrderSide Side => OrderSide.Buy;
    }
}

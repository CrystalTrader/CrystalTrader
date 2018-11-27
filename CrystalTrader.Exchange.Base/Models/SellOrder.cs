using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Exchange.Base
{
    public class SellOrder : Order
    {
        public override OrderSide Side => OrderSide.Sell;
    }
}

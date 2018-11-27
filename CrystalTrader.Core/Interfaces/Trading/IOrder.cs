using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IOrder
    {
        OrderSide Side { get; }
        OrderType Type { get; }
        DateTimeOffset Date { get; }
        string Pair { get; }
        decimal Amount { get; }
        decimal Price { get; }
    }
}

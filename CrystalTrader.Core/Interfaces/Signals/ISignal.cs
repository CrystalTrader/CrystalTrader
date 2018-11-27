using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ISignal
    {
        string Name { get; }
        string Pair { get; }
        long? Volume { get; }
        double? VolumeChange { get; set; }
        decimal? Price { get; }
        decimal? PriceChange { get; }
        double? Rating { get; }
        double? RatingChange { get; }
        double? Volatility { get; }
    }
}

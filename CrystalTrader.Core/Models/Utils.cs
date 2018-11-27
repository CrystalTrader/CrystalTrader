﻿namespace CrystalTrader.Core
{
    public static class Utils
    {
        public static decimal CalculatePercentage(decimal oldValue, decimal newValue)
        {
            if (oldValue != 0)
            {
                return (newValue - oldValue) / oldValue * 100;
            }
            else
            {
                return 0;
            }
        }
    }
}

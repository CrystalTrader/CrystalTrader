﻿using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Backtesting
{
    internal class BacktestingConfig : IBacktestingConfig
    {
        public bool Enabled { get; set; }
        public bool Replay { get; set; }
        public bool ReplayOutput { get; set; }
        public double ReplaySpeed { get; set; }
        public int? ReplayStartIndex { get; set; }
        public int? ReplayEndIndex { get; set; }
        public bool DeleteLogs { get; set; }
        public bool DeleteAccountData { get; set; }
        public string CopyAccountDataPath { get; set; }
        public int TradingSpeedEasing { get; set; }
        public int TradingRulesSpeedEasing { get; set; }
        public int SignalRulesSpeedEasing { get; set; }
        public int SnapshotsInterval { get; set; }
        public string SnapshotsPath { get; set; }
    }
}

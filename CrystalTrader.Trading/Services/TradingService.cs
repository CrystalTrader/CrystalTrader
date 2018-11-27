﻿using CrystalTrader.Core;
using CrystalTrader.Exchange.Base;
using CrystalTrader.Signals.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CrystalTrader.Trading
{
    internal class TradingService : ConfigrableServiceBase<TradingConfig>, ITradingService
    {
        private const int MIN_INTERVAL_BETWEEN_BUY_AND_SELL = 7000;
        private const decimal DEFAULT_ARBITRAGE_BUY_MULTIPLIER = 0.99M;
        private const decimal DEFAULT_ARBITRAGE_SELL_MULTIPLIER = 0.99M;

        public override string ServiceName => Constants.ServiceNames.TradingService;

        ITradingConfig ITradingService.Config => Config;
        public IModuleRules Rules { get; private set; }
        public TradingRulesConfig RulesConfig { get; private set; }

        public IExchangeService Exchange { get; private set; }
        public ITradingAccount Account { get; private set; }
        public ConcurrentStack<IOrderDetails> OrderHistory { get; private set; } = new ConcurrentStack<IOrderDetails>();
        public bool IsTradingSuspended { get; private set; }

        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITasksService tasksService;
        private IOrderingService orderingService;
        private IRulesService rulesService;
        private ISignalsService signalsService;

        private TradingTimedTask tradingTimedTask;
        private TradingRulesTimedTask tradingRulesTimedTask;
        private AccountRefreshTimedTask accountRefreshTimedTask;

        private bool tradingForcefullySuspended;
        private object syncRoot = new object();

        public TradingService(ILoggingService loggingService, INotificationService notificationService, IHealthCheckService healthCheckService, ITasksService tasksService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.healthCheckService = healthCheckService;
            this.tasksService = tasksService;

            var isBacktesting = Application.Resolve<IBacktestingService>().Config.Enabled && Application.Resolve<IBacktestingService>().Config.Replay;
            if (isBacktesting)
            {
                this.Exchange = Application.ResolveOptionalNamed<IExchangeService>(Constants.ServiceNames.BacktestingExchangeService);
            }
            else
            {
                this.Exchange = Application.ResolveOptionalNamed<IExchangeService>(Config.Exchange);
            }

            if (this.Exchange == null)
            {
                throw new Exception($"Unsupported exchange: {Config.Exchange}");
            }
        }

        public void Start()
        {
            loggingService.Info($"Start Trading service (Virtual: {Config.VirtualTrading})...");

            IsTradingSuspended = true;

            orderingService = Application.Resolve<IOrderingService>();
            rulesService = Application.Resolve<IRulesService>();
            OnTradingRulesChanged();
            rulesService.RegisterRulesChangeCallback(OnTradingRulesChanged);
            Exchange.Start(Config.VirtualTrading);
            signalsService = Application.Resolve<ISignalsService>();

            if (!Config.VirtualTrading)
            {
                Account = new ExchangeAccount(loggingService, notificationService, healthCheckService, signalsService, this);
            }
            else
            {
                Account = new VirtualAccount(loggingService, notificationService, healthCheckService, signalsService, this);
            }

            accountRefreshTimedTask = tasksService.AddTask(
                name: nameof(AccountRefreshTimedTask),
                task: new AccountRefreshTimedTask(loggingService, healthCheckService, this),
                interval: Config.AccountRefreshInterval * 1000 / Application.Speed,
                startDelay: Constants.TaskDelays.ZeroDelay,
                startTask: false,
                runNow: true,
                skipIteration: 0);

            if (signalsService.Config.Enabled)
            {
                signalsService.Start();
            }

            tradingTimedTask = tasksService.AddTask(
                name: nameof(TradingTimedTask),
                task: new TradingTimedTask(loggingService, notificationService, healthCheckService, signalsService, orderingService, this),
                interval: Config.TradingCheckInterval * 1000 / Application.Speed,
                startDelay: Constants.TaskDelays.NormalDelay,
                startTask: false,
                runNow: false,
                skipIteration: 0);

            tradingRulesTimedTask = tasksService.AddTask(
                name: nameof(TradingRulesTimedTask),
                task: new TradingRulesTimedTask(loggingService, notificationService, healthCheckService, rulesService, signalsService, this),
                interval: RulesConfig.CheckInterval * 1000 / Application.Speed,
                startDelay: Constants.TaskDelays.MidDelay,
                startTask: false,
                runNow: false,
                skipIteration: 0);

            IsTradingSuspended = false;

            loggingService.Info("Trading service started");
        }

        public void Stop()
        {
            loggingService.Info("Stop Trading service...");

            Exchange.Stop();

            if (signalsService.Config.Enabled)
            {
                signalsService.Stop();
            }

            tasksService.RemoveTask(nameof(TradingTimedTask), stopTask: true);
            tasksService.RemoveTask(nameof(TradingRulesTimedTask), stopTask: true);
            tasksService.RemoveTask(nameof(AccountRefreshTimedTask), stopTask: true);

            Account.Dispose();

            rulesService.UnregisterRulesChangeCallback(OnTradingRulesChanged);

            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingRulesProcessed);
            healthCheckService.RemoveHealthCheck(Constants.HealthChecks.TradingPairsProcessed);

            loggingService.Info("Trading service stopped");
        }

        public void ResumeTrading(bool forced)
        {
            if (IsTradingSuspended && (!tradingForcefullySuspended || forced))
            {
                loggingService.Info("Trading started");
                IsTradingSuspended = false;

                tradingTimedTask.Start();
                tradingRulesTimedTask.Start();
                tradingRulesTimedTask.RunNow();
            }
        }

        public void SuspendTrading(bool forced)
        {
            if (!IsTradingSuspended)
            {
                loggingService.Info("Trading suspended");
                IsTradingSuspended = true;
                tradingForcefullySuspended = forced;

                tradingRulesTimedTask.Stop();
                tradingTimedTask.Stop();
                tradingTimedTask.StopTrailing();
            }
        }

        public IPairConfig GetPairConfig(string pair)
        {
            return tradingRulesTimedTask.GetPairConfig(pair);
        }

        public void ReapplyTradingRules()
        {
            tradingRulesTimedTask.RunNow();
        }

        public void Buy(BuyOptions options)
        {
            lock (syncRoot)
            {
                PauseTasks();
                try
                {
                    IRule rule = signalsService.Rules.Entries.FirstOrDefault(r => r.Name == options.Metadata.SignalRule);
                    RuleAction ruleAction = rule?.Action ?? RuleAction.Default;
                    IPairConfig pairConfig = GetPairConfig(options.Pair);

                    bool arbitragePair = pairConfig.ArbitrageEnabled && pairConfig.ArbitrageSignalRules.Contains(options.Metadata.SignalRule);
                    if (arbitragePair)
                    {
                        Arbitrage arbitrage = Exchange.GetArbitrage(options.Pair, Config.Market, pairConfig.ArbitrageMarkets, pairConfig.ArbitrageType);
                        if (arbitrage.IsAssigned)
                        {
                            Arbitrage(new ArbitrageOptions(options.Pair, arbitrage, options.Metadata));
                        }
                    }
                    else
                    {
                        ITradingPair swappedPair = Account.GetTradingPairs().OrderBy(p => p.CurrentMargin).FirstOrDefault(tradingPair =>
                        {
                            IPairConfig tradingPairConfig = GetPairConfig(tradingPair.Pair);
                            return tradingPairConfig.SellEnabled && tradingPairConfig.SwapEnabled && tradingPairConfig.SwapSignalRules != null &&
                                   tradingPairConfig.SwapSignalRules.Contains(options.Metadata.SignalRule) &&
                                   tradingPairConfig.SwapTimeout < (DateTimeOffset.Now - tradingPair.OrderDates.DefaultIfEmpty().Max()).TotalSeconds;
                        });

                        if (swappedPair != null)
                        {
                            Swap(new SwapOptions(swappedPair.Pair, options.Pair, options.Metadata));
                        }
                        else if (ruleAction == RuleAction.Default)
                        {
                            if (CanBuy(options, out string message))
                            {
                                tradingTimedTask.InitiateBuy(options);
                            }
                            else
                            {
                                loggingService.Debug(message);
                            }
                        }
                    }
                }
                finally
                {
                    ContinueTasks();
                }
            }
        }

        public void Sell(SellOptions options)
        {
            lock (syncRoot)
            {
                PauseTasks();
                try
                {
                    if (CanSell(options, out string message))
                    {
                        tradingTimedTask.InitiateSell(options);
                    }
                    else
                    {
                        loggingService.Debug(message);
                    }
                }
                finally
                {
                    ContinueTasks();
                }
            }
        }

        public void Swap(SwapOptions options)
        {
            lock (syncRoot)
            {
                PauseTasks();
                try
                {
                    if (CanSwap(options, out string message))
                    {
                        ITradingPair oldTradingPair = Account.GetTradingPair(options.OldPair);
                        var sellOptions = new SellOptions(options.OldPair)
                        {
                            Swap = true,
                            ManualOrder = options.ManualOrder,
                            Metadata = new OrderMetadata { SwapPair = options.NewPair }
                        };

                        if (CanSell(sellOptions, out message))
                        {
                            decimal currentMargin = oldTradingPair.CurrentMargin;
                            decimal additionalCosts = oldTradingPair.Cost - oldTradingPair.CurrentCost + (oldTradingPair.Metadata.AdditionalCosts ?? 0);
                            int additionalDCALevels = oldTradingPair.DCALevel;

                            IOrderDetails sellOrderDetails = orderingService.PlaceSellOrder(sellOptions);
                            if (!Account.HasTradingPair(options.OldPair))
                            {
                                var buyOptions = new BuyOptions(options.NewPair)
                                {
                                    Swap = true,
                                    ManualOrder = options.ManualOrder,
                                    MaxCost = sellOrderDetails.Cost,
                                    Metadata = options.Metadata
                                };
                                buyOptions.Metadata.LastBuyMargin = currentMargin;
                                buyOptions.Metadata.SwapPair = options.OldPair;
                                buyOptions.Metadata.AdditionalDCALevels = additionalDCALevels;
                                buyOptions.Metadata.AdditionalCosts = additionalCosts;
                                IOrderDetails buyOrderDetails = orderingService.PlaceBuyOrder(buyOptions);

                                var newTradingPair = Account.GetTradingPair(options.NewPair) as TradingPair;
                                if (newTradingPair != null)
                                {
                                    newTradingPair.Metadata.AdditionalCosts += CalculateOrderFees(sellOrderDetails);
                                    loggingService.Info($"Swap {oldTradingPair.FormattedName} for {newTradingPair.FormattedName}. " +
                                        $"Old margin: {oldTradingPair.CurrentMargin:0.00}, new margin: {newTradingPair.CurrentMargin:0.00}");
                                }
                                else
                                {
                                    loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}. Reason: failed to buy {options.NewPair}");
                                    notificationService.Notify($"Unable to swap {options.OldPair} for {options.NewPair}: Failed to buy {options.NewPair}");
                                }
                            }
                            else
                            {
                                loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}. Reason: failed to sell {options.OldPair}");
                            }
                        }
                        else
                        {
                            loggingService.Info($"Unable to swap {options.OldPair} for {options.NewPair}: {message}");
                        }
                    }
                    else
                    {
                        loggingService.Info(message);
                    }
                }
                finally
                {
                    ContinueTasks();
                }
            }
        }

        public void Arbitrage(ArbitrageOptions options)
        {
            lock (syncRoot)
            {
                PauseTasks();
                try
                {
                    if (CanArbitrage(options, out string message))
                    {
                        if (CanBuy(new BuyOptions(options.Pair) { Amount = 1 }, out message))
                        {
                            options.Metadata.Arbitrage = $"{options.Arbitrage.Market}-" + options.Arbitrage.Type ?? "All";
                            options.Metadata.ArbitragePercentage = options.Arbitrage.Percentage;
                            loggingService.Info($"{options.Arbitrage.Type} arbitrage {options.Pair} on {options.Arbitrage.Market}. Percentage: {options.Arbitrage.Percentage:0.00}");

                            if (options.Arbitrage.Type == ArbitrageType.Direct)
                            {
                                ArbitrageDirect(options);
                            }
                            else if (options.Arbitrage.Type == ArbitrageType.Reverse)
                            {
                                ArbitrageReverse(options);
                            }
                        }
                        else
                        {
                            loggingService.Info($"Unable to arbitrage {options.Pair}: {message}");
                        }
                    }
                    else
                    {
                        loggingService.Info(message);
                    }
                }
                finally
                {
                    ContinueTasks();
                }
            }
        }

        private void ArbitrageDirect(ArbitrageOptions options)
        {
            string arbitragePair = options.Pair;
            ITradingPair existingArbitragePair = Account.GetTradingPair(arbitragePair);
            IPairConfig pairConfig = GetPairConfig(options.Pair);
            bool useExistingArbitragePair = (existingArbitragePair != null && existingArbitragePair.CurrentCost > pairConfig.BuyMaxCost &&
                                            existingArbitragePair.AveragePrice <= existingArbitragePair.CurrentPrice);

            var buyArbitragePairOptions = new BuyOptions(arbitragePair)
            {
                Arbitrage = true,
                MaxCost = pairConfig.BuyMaxCost,
                ManualOrder = options.ManualOrder,
                IgnoreBalance = useExistingArbitragePair,
                Metadata = options.Metadata
            };

            if (CanBuy(buyArbitragePairOptions, out string message))
            {
                IOrderDetails buyArbitragePairOrderDetails = null;
                if (useExistingArbitragePair)
                {
                    buyArbitragePairOrderDetails = Account.AddBlankOrder(buyArbitragePairOptions.Pair,
                        buyArbitragePairOptions.MaxCost.Value / GetPrice(buyArbitragePairOptions.Pair, TradePriceType.Ask),
                        includeFees: false);
                    loggingService.Info($"Use existing arbitrage pair for arbitrage: {arbitragePair}. " +
                        $"Average price: {existingArbitragePair.AveragePrice}, Current price: {existingArbitragePair.CurrentPrice}");
                }
                else
                {
                    buyArbitragePairOrderDetails = orderingService.PlaceBuyOrder(buyArbitragePairOptions);
                }

                if (buyArbitragePairOrderDetails.Result == OrderResult.Filled)
                {
                    decimal buyArbitragePairFees = CalculateOrderFees(buyArbitragePairOrderDetails);
                    string flippedArbitragePair = Exchange.ChangeMarket(arbitragePair, options.Arbitrage.Market.ToString());
                    var sellArbitragePairOptions = new SellOptions(flippedArbitragePair)
                    {
                        Arbitrage = true,
                        Amount = buyArbitragePairOrderDetails.AmountFilled,
                        ManualOrder = options.ManualOrder,
                        Metadata = options.Metadata.MergeWith(new OrderMetadata
                        {
                            IsTransitional = true
                        })
                    };

                    IOrderDetails sellArbitragePairOrderDetails = orderingService.PlaceSellOrder(sellArbitragePairOptions);
                    if (sellArbitragePairOrderDetails.Result == OrderResult.Filled)
                    {
                        decimal sellArbitragePairMultiplier = pairConfig.ArbitrageSellMultiplier ?? DEFAULT_ARBITRAGE_SELL_MULTIPLIER;
                        decimal sellArbitragePairFees = CalculateOrderFees(sellArbitragePairOrderDetails);
                        options.Metadata.FeesNonDeductible = buyArbitragePairFees  * sellArbitragePairMultiplier;
                        decimal sellMarketPairAmount = sellArbitragePairOrderDetails.AmountFilled * GetPrice(flippedArbitragePair, TradePriceType.Bid, normalize: false) * sellArbitragePairMultiplier;
                        string marketPair = Exchange.GetArbitrageMarketPair(options.Arbitrage.Market);

                        var sellMarketPairOptions = new SellOptions(marketPair)
                        {
                            Arbitrage = true,
                            Amount = sellMarketPairAmount,
                            ManualOrder = options.ManualOrder,
                            Metadata = options.Metadata.MergeWith(new OrderMetadata
                            {
                                IsTransitional = false,
                                OriginalPair = arbitragePair
                            })
                        };

                        existingArbitragePair = Account.GetTradingPair(marketPair);
                        existingArbitragePair.OverrideCost((buyArbitragePairOrderDetails.Cost + sellArbitragePairFees * 2) * sellArbitragePairMultiplier);
                        IOrderDetails sellMarketPairOrderDetails = orderingService.PlaceSellOrder(sellMarketPairOptions);
                        existingArbitragePair.OverrideCost(null);

                        if (sellMarketPairOrderDetails.Result == OrderResult.Filled)
                        {
                            loggingService.Info($"{pairConfig.ArbitrageType} arbitrage successful: {arbitragePair} -> {flippedArbitragePair} -> {marketPair}");
                        }
                        else
                        {
                            loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to sell market pair {arbitragePair}");
                            notificationService.Notify($"Unable to arbitrage {options.Pair}: Failed to sell market pair {arbitragePair}");
                        }
                    }
                    else
                    {
                        loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to sell arbitrage pair {flippedArbitragePair}");
                        notificationService.Notify($"Unable to arbitrage {options.Pair}: Failed to sell arbitrage pair {flippedArbitragePair}");
                    }
                }
                else
                {
                    loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to buy arbitrage pair {arbitragePair}");
                }
            }
            else
            {
                loggingService.Info($"Unable to arbitrage {options.Pair}: {message}");
            }
        }

        private void ArbitrageReverse(ArbitrageOptions options)
        {
            string marketPair = Exchange.GetArbitrageMarketPair(options.Arbitrage.Market);
            ITradingPair existingMarketPair = Account.GetTradingPair(marketPair);
            IPairConfig pairConfig = GetPairConfig(options.Pair);
            bool useExistingMarketPair = (existingMarketPair != null && existingMarketPair.CurrentCost > pairConfig.BuyMaxCost &&
                                         existingMarketPair.AveragePrice <= existingMarketPair.CurrentPrice);

            var buyMarketPairOptions = new BuyOptions(marketPair)
            {
                Arbitrage = true,
                MaxCost = pairConfig.BuyMaxCost,
                ManualOrder = options.ManualOrder,
                IgnoreBalance = useExistingMarketPair,
                Metadata = options.Metadata
            };

            if (CanBuy(buyMarketPairOptions, out string message))
            {
                IOrderDetails buyMarketPairOrderDetails = null;
                if (useExistingMarketPair)
                {
                    buyMarketPairOrderDetails = Account.AddBlankOrder(buyMarketPairOptions.Pair,
                        buyMarketPairOptions.MaxCost.Value / GetPrice(buyMarketPairOptions.Pair, TradePriceType.Ask),
                        includeFees: false);
                    loggingService.Info($"Use existing market pair for arbitrage: {marketPair}. " +
                        $"Average price: {existingMarketPair.AveragePrice}, Current price: {existingMarketPair.CurrentPrice}");
                }
                else
                {
                    buyMarketPairOrderDetails = orderingService.PlaceBuyOrder(buyMarketPairOptions);
                }

                if (buyMarketPairOrderDetails.Result == OrderResult.Filled)
                {
                    decimal buyArbitragePairMultiplier = pairConfig.ArbitrageBuyMultiplier ?? DEFAULT_ARBITRAGE_BUY_MULTIPLIER;
                    decimal buyMarketPairFees = CalculateOrderFees(buyMarketPairOrderDetails);
                    string arbitragePair = Exchange.ChangeMarket(options.Pair, options.Arbitrage.Market.ToString());
                    decimal buyArbitragePairAmount = options.Arbitrage.Market == ArbitrageMarket.USDT ?
                        buyMarketPairOrderDetails.AmountFilled * GetPrice(buyMarketPairOrderDetails.Pair, TradePriceType.Ask, normalize: false) / GetPrice(arbitragePair, TradePriceType.Ask) :
                        buyMarketPairOrderDetails.AmountFilled / GetPrice(arbitragePair, TradePriceType.Ask);

                    var buyArbitragePairOptions = new BuyOptions(arbitragePair)
                    {
                        Arbitrage = true,
                        ManualOrder = options.ManualOrder,
                        Amount = buyArbitragePairAmount * buyArbitragePairMultiplier,
                        Metadata = options.Metadata
                    };

                    IOrderDetails buyArbitragePairOrderDetails = orderingService.PlaceBuyOrder(buyArbitragePairOptions);
                    if (buyArbitragePairOrderDetails.Result == OrderResult.Filled)
                    {
                        decimal buyArbitragePairFees = CalculateOrderFees(buyArbitragePairOrderDetails);
                        options.Metadata.FeesNonDeductible = buyMarketPairFees * buyArbitragePairMultiplier;
                        var sellArbitragePairOptions = new SellOptions(buyArbitragePairOrderDetails.Pair)
                        {
                            Arbitrage = true,
                            Amount = buyArbitragePairOrderDetails.AmountFilled,
                            ManualOrder = options.ManualOrder,
                            Metadata = options.Metadata
                        };

                        TradingPair existingArbitragePair = Account.GetTradingPair(buyArbitragePairOrderDetails.Pair) as TradingPair;
                        existingArbitragePair.OverrideCost(buyArbitragePairOrderDetails.Cost + buyArbitragePairFees * 2);
                        IOrderDetails sellArbitragePairOrderDetails = orderingService.PlaceSellOrder(sellArbitragePairOptions);
                        existingArbitragePair.OverrideCost(null);

                        if (sellArbitragePairOrderDetails.Result == OrderResult.Filled)
                        {
                            loggingService.Info($"{pairConfig.ArbitrageType} arbitrage successful: {marketPair} -> {arbitragePair} -> {existingArbitragePair.Pair}");
                        }
                        else
                        {
                            loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to sell arbitrage pair {arbitragePair}");
                            notificationService.Notify($"Unable to arbitrage {options.Pair}: Failed to sell arbitrage pair {arbitragePair}");
                        }
                    }
                    else
                    {
                        loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to buy arbitrage pair {arbitragePair}");
                        notificationService.Notify($"Unable to arbitrage {options.Pair}: Failed to buy arbitrage pair {arbitragePair}");
                    }
                }
                else
                {
                    loggingService.Info($"Unable to arbitrage {options.Pair}. Reason: failed to buy market pair {marketPair}");
                }
            }
            else
            {
                loggingService.Info($"Unable to arbitrage {options.Pair}: {message}");
            }
        }

        public bool CanBuy(BuyOptions options, out string message)
        {
            IPairConfig pairConfig = GetPairConfig(options.Pair);

            if (!options.ManualOrder && !options.Swap && IsTradingSuspended)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: trading suspended";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && !pairConfig.BuyEnabled)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: buying not enabled";
                return false;
            }
            else if (!options.ManualOrder && Config.ExcludedPairs.Contains(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: exluded pair";
                return false;
            }
            else if (!options.ManualOrder && !options.Arbitrage && !options.IgnoreExisting && Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: pair already exists";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && !options.Arbitrage && pairConfig.MaxPairs != 0 && Account.GetTradingPairs().Count() >= pairConfig.MaxPairs && !Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel buy request for {options.Pair}. Reason: maximum pairs reached";
                return false;
            }
            else if (!options.ManualOrder && !options.Swap && !options.IgnoreBalance && pairConfig.BuyMinBalance != 0 && (Account.GetBalance() - options.MaxCost) < pairConfig.BuyMinBalance && Exchange.GetPairMarket(options.Pair) == Config.Market)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: minimum balance reached";
                return false;
            }
            else if (options.Price != null && options.Price <= 0)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: invalid price";
                return false;
            }
            else if (options.Amount != null && options.Amount <= 0)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: invalid amount";
                return false;
            }
            else if (!options.IgnoreBalance && Account.GetBalance() < options.MaxCost && Exchange.GetPairMarket(options.Pair) == Config.Market)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: not enough balance";
                return false;
            }
            else if (options.Amount == null && options.MaxCost == null || options.Amount != null && options.MaxCost != null)
            {
                message = $"Cancel buy request for {options.Pair}. Reason: either max cost or amount needs to be specified (not both)";
            }
            else if (!options.ManualOrder && !options.Swap && !options.Arbitrage && pairConfig.BuySamePairTimeout > 0 &&
                OrderHistory.Any(h => h.Side == OrderSide.Buy && (h.Pair == options.Pair || h.Pair == h.OriginalPair)) &&
                (DateTimeOffset.Now - OrderHistory.Where(h => (h.Pair == options.Pair || h.Pair == h.OriginalPair)).Max(h => h.Date)).TotalSeconds < pairConfig.BuySamePairTimeout)
            {
                var elapsedSeconds = (DateTimeOffset.Now - OrderHistory.Where(h => (h.Pair == options.Pair || h.Pair == h.OriginalPair)).Max(h => h.Date)).TotalSeconds;
                message = $"Cancel buy request for {options.Pair}. Reason: buy same pair timeout (elapsed: {elapsedSeconds:0.#}, timeout: {pairConfig.BuySamePairTimeout:0.#})";
                return false;
            }

            message = null;
            return true;
        }

        public bool CanSell(SellOptions options, out string message)
        {
            IPairConfig pairConfig = GetPairConfig(options.Pair);

            if (!options.ManualOrder && !options.Arbitrage && IsTradingSuspended)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: trading suspended";
                return false;
            }
            else if (!options.ManualOrder && !options.Arbitrage && !pairConfig.SellEnabled)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: selling not enabled";
                return false;
            }
            else if (!options.ManualOrder && !options.Arbitrage && Config.ExcludedPairs.Contains(options.Pair))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: excluded pair";
                return false;
            }
            else if (!Account.HasTradingPair(options.Pair, includeDust: true) && !Account.HasTradingPair(NormalizePair(options.Pair), includeDust: true))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: pair does not exist";
                return false;
            }
            else if (options.Price != null && options.Price <= 0)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: invalid price";
                return false;
            }
            else if (options.Amount != null && options.Amount <= 0)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: invalid amount";
                return false;
            }
            else if (options.Amount != null && options.Price != null && (options.Amount * options.Price) < Config.MinCost)
            {
                message = $"Cancel sell request for {options.Pair}. Reason: dust";
                return false;
            }
            else if (!options.ManualOrder && !options.Arbitrage && (DateTimeOffset.Now - Account.GetTradingPair(options.Pair, includeDust: true).OrderDates.DefaultIfEmpty().Max()).
                TotalMilliseconds < (MIN_INTERVAL_BETWEEN_BUY_AND_SELL / Application.Speed))
            {
                message = $"Cancel sell request for {options.Pair}. Reason: pair just bought";
                return false;
            }
            message = null;
            return true;
        }

        public bool CanSwap(SwapOptions options, out string message)
        {
            if (!Account.HasTradingPair(options.OldPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: pair does not exist";
                return false;
            }
            else if (Account.HasTradingPair(options.NewPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: pair already exists";
                return false;
            }
            else if (!options.ManualOrder && !GetPairConfig(options.OldPair).SellEnabled)
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: selling not enabled";
                return false;
            }
            else if (!options.ManualOrder && !GetPairConfig(options.NewPair).BuyEnabled)
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: buying not enabled";
                return false;
            }
            else if (Account.GetBalance() < Account.GetTradingPair(options.OldPair).CurrentCost * 0.01M)
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: not enough balance";
                return false;
            }
            else if (!Exchange.GetMarketPairs(Config.Market).Contains(options.NewPair))
            {
                message = $"Cancel swap request {options.OldPair} for {options.NewPair}. Reason: {options.NewPair} is not a valid pair";
                return false;
            }

            message = null;
            return true;
        }

        public bool CanArbitrage(ArbitrageOptions options, out string message)
        {
            if (Account.HasTradingPair(options.Pair))
            {
                message = $"Cancel arbitrage request {options.Pair}. Reason: pair already exist";
                return false;
            }
            else if (!options.ManualOrder && !GetPairConfig(options.Pair).BuyEnabled)
            {
                message = $"Cancel arbitrage request for {options.Pair}. Reason: buying not enabled";
                return false;
            }
            else if (!Exchange.GetMarketPairs(Config.Market).Contains(options.Pair))
            {
                message = $"Cancel arbitrage request for {options.Pair}. Reason: {options.Pair} is not a valid pair";
                return false;
            }

            message = null;
            return true;
        }

        public decimal GetPrice(string pair, TradePriceType? priceType = null, bool normalize = true)
        {
            if (normalize)
            {
                if (pair == Config.Market + Constants.Markets.USDT)
                {
                    return 1;
                }
            }
            return Exchange.GetPrice(pair, priceType ?? Config.TradePriceType);
        }

        public decimal CalculateOrderFees(IOrderDetails order)
        {
            decimal orderFees = 0;
            if (order.Fees != 0 && order.FeesCurrency != null)
            {
                if (order.FeesCurrency == Config.Market)
                {
                    orderFees = order.Fees;
                }
                else
                {
                    string feesPair = order.FeesCurrency + Config.Market;
                    orderFees = GetPrice(feesPair, TradePriceType.Ask) * order.Fees;
                }
            }
            return orderFees;
        }

        public bool IsNormalizedPair(string pair)
        {
            return Exchange.GetPairMarket(pair) == Config.Market;
        }

        public string NormalizePair(string pair)
        {
            return Exchange.ChangeMarket(pair, Config.Market);
        }

        public void LogOrder(IOrderDetails order)
        {
            OrderHistory.Push(order);
        }

        public List<string> GetTrailingBuys()
        {
            return tradingTimedTask.GetTrailingBuys();
        }

        public List<string> GetTrailingSells()
        {
            return tradingTimedTask.GetTrailingSells();
        }

        public void StopTrailingBuy(string pair)
        {
            tradingTimedTask.StopTrailingBuy(pair);
        }

        public void StopTrailingSell(string pair)
        {
            tradingTimedTask.StopTrailingSell(pair);
        }

        private void OnTradingRulesChanged()
        {
            Rules = rulesService.GetRules(ServiceName);
            RulesConfig = Rules.GetConfiguration<TradingRulesConfig>();
        }

        protected override void PrepareConfig()
        {
            if (Config.ExcludedPairs == null)
            {
                Config.ExcludedPairs = new List<string>();
            }

            if (Config.DCALevels == null)
            {
                Config.DCALevels = new List<DCALevel>();
            }
        }

        private void PauseTasks()
        {
            tasksService.GetTask(nameof(TradingTimedTask)).Pause();
            tasksService.GetTask(nameof(TradingRulesTimedTask)).Pause();
            tasksService.GetTask(nameof(SignalRulesTimedTask)).Pause();
            tasksService.GetTask("BacktestingLoadSnapshotsTimedTask")?.Pause();
        }

        private void ContinueTasks()
        {
            tasksService.GetTask(nameof(TradingTimedTask)).Continue();
            tasksService.GetTask(nameof(TradingRulesTimedTask)).Continue();
            tasksService.GetTask(nameof(SignalRulesTimedTask)).Continue();
            tasksService.GetTask("BacktestingLoadSnapshotsTimedTask")?.Continue();
        }
    }
}

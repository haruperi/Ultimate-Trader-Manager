using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.EEuropeStandardTime, AccessRights = AccessRights.FullAccess)]
    public class UltimateTraderManager : Robot
    {
        #region Identity

        public const string NAME = "RH Position Trader cBot";

        public const string VERSION = "1.0";

        #endregion

        #region Enum
        public enum OpenTradeType
        {
            All,
            Buy,
            Sell
        }

        public enum LoopMode
        {
            OnTick,
            OnBar,
            OnTimer
        }

        public enum TradingMode
        {
            Auto,
            Manual,
            Both
        }

        public enum AutoStrategyName
        {
            Trend_MA,
            Trend_MA_MTF,
            HHLL,
            RSIReversion,
            ADRReversion
        }

        public enum RiskBase
        {
            BaseEquity,
            BaseBalance,
            BaseMargin
        };

        public enum PositionSizeMode
        {
            Risk_Fixed,
            Risk_Auto,
        };

        public enum StopLossMode
        {
            SL_None,
            SL_Fixed,
            SL_Auto_ADR,
        };

        public enum TakeProfitMode
        {
            TP_None,
            TP_Fixed,
            TP_Auto_ADR,
            TP_Auto_RRR
        };
        #endregion

        #region Parameters of CBot

        #region Identity
        [Parameter(NAME + " " + VERSION, Group = "IDENTITY", DefaultValue = "https://haruperi.ltd/trading/")]
        public string ProductInfo { get; set; }

        [Parameter("Preset information", Group = "IDENTITY", DefaultValue = "XAUUSD Range5 | 01.01.2024 to 29.04.2024 | $1000")]
        public string PresetInfo { get; set; }
        #endregion

        #region Strategy
        [Parameter("Loop Mode", Group = "STRATEGY", DefaultValue = LoopMode.OnBar)]
        public LoopMode MyLoopMode { get; set; }

        [Parameter("Open Trade Type", Group = "STRATEGY", DefaultValue = OpenTradeType.All)]
        public OpenTradeType MyOpenTradeType { get; set; }

        [Parameter("Trading Mode", Group = "STRATEGY", DefaultValue = TradingMode.Both)]
        public TradingMode MyTradingMode { get; set; }

        [Parameter("Auto Strategy Name", Group = "STRATEGY", DefaultValue = AutoStrategyName.Trend_MA)]
        public AutoStrategyName MyAutoStrategyName { get; set; }
        #endregion

        #region Trading Hours
        [Parameter("Use Trading Hours", Group = "TRADING HOURS", DefaultValue = false)]
        public bool UseTradingHours { get; set; }

        [Parameter("Starting Hour", Group = "TRADING HOURS", DefaultValue = 09)]
        public int TradingHourStart { get; set; }

        [Parameter("Ending Hour", Group = "TRADING HOURS", DefaultValue = 18)]
        public int TradingHourEnd { get; set; }
        #endregion

        #region Risk Management Settings
        [Parameter("Position size mode", Group = "RISK MANAGEMENT", DefaultValue = PositionSizeMode.Risk_Fixed)]
        public PositionSizeMode MyPositionSizeMode { get; set; }

        [Parameter("Default Lot Size", Group = "RISK MANAGEMENT", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double DefaultLotSize { get; set; }

        [Parameter("Cal Risk From ", Group = "RISK MANAGEMENT", DefaultValue = RiskBase.BaseBalance)]
        public RiskBase MyRiskBase { get; set; }

        [Parameter("Max Risk % Per Trade", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.1, Step = 0.01)]
        public double MaxRiskPerTrade { get; set; }

        [Parameter("Risk Reward Ratio - 1:", Group = "RISK MANAGEMENT", DefaultValue = 2, MinValue = 0.1, Step = 0.01)]
        public double RiskRewardRatio { get; set; }

        [Parameter("Max Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Max Buy Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxBuyPositions { get; set; }

        [Parameter("Max Sell Positions", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 1, Step = 1)]
        public int MaxSellPositions { get; set; }

        [Parameter("Stop Loss Mode", Group = "RISK MANAGEMENT", DefaultValue = StopLossMode.SL_Fixed)]
        public StopLossMode MyStopLossMode { get; set; }

        [Parameter("Default StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = 20, MinValue = 5, Step = 1)]
        public double DefaultStopLoss { get; set; }

        [Parameter("Take Profit Mode", Group = "RISK MANAGEMENT", DefaultValue = TakeProfitMode.TP_Fixed)]
        public TakeProfitMode MyTakeProfitMode { get; set; }

        [Parameter("Default Take Profit", Group = "RISK MANAGEMENT", DefaultValue = 21, MinValue = 5, Step = 1)]
        public double DefaultTakeProfit { get; set; }
        #endregion

        #region Trade Management
        [Parameter("Use Auto Trade Management", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool AutoTradeManagement { get; set; }

        [Parameter("Use Pyramids for TP", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool DoPyramidsTrading { get; set; }

        [Parameter("Equity Target", Group = "TRADE MANAGEMENT", DefaultValue = 100000)]
        public double EquityTarget { get; set; }

        [Parameter("Cost Ave Distance", Group = "TRADE MANAGEMENT", DefaultValue = 20)]
        public double CostAveDistance { get; set; }

        [Parameter("Pyramid Distance", Group = "TRADE MANAGEMENT", DefaultValue = 20)]
        public double PyramidDistance { get; set; }

        [Parameter("Cost Ave Distance Multiplier", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double CostAveMultiplier { get; set; }

        [Parameter("Pyramid Lot Divisor", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double PyramidLotDivisor { get; set; }

        [Parameter("Pyramid Stop Loss", Group = "TRADE MANAGEMENT", DefaultValue = 5)]
        public double PyramidStopLoss { get; set; }

        [Parameter("Use Trailing Stop ", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool UseTrailingStop { get; set; }

        [Parameter("Trail After (Pips) ", Group = "TRADE MANAGEMENT", DefaultValue = 100)]
        public double WhenToTrail { get; set; }

        [Parameter("Break-Even Losing Trades", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool BreakEvenLosing { get; set; }

        [Parameter("Cost Ave Take Profit", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool IsCostAveTakeProfit { get; set; }

        [Parameter("Break-Even Extra (pips)", Group = "TRADE MANAGEMENT", DefaultValue = 1, MinValue = 1)]
        public int BreakEvenExtraPips { get; set; }
        #endregion

        #region  Indicator Settings
        [Parameter("ADRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 10)]
        public int ADRPeriod { get; set; }

        [Parameter("ADR Divisor SL", Group = "INDICATOR SETTINGS", DefaultValue = 3)]
        public double ADR_SL { get; set; }

        [Parameter("ADR Divisor TP", Group = "INDICATOR SETTINGS", DefaultValue = 2)]
        public double ADR_TP { get; set; }

        [Parameter("Lower Timeframe", Group = "INDICATOR SETTINGS", DefaultValue = "Minute5")]
        public TimeFrame LowerTimeframe { get; set; }

        [Parameter("Higher Timeframe", Group = "INDICATOR SETTINGS", DefaultValue = "Hour")]
        public TimeFrame HigherTimeframe { get; set; }

        [Parameter("Period Fast MA", Group = "INDICATOR SETTINGS", DefaultValue = 3)]
        public int PeriodFastMA { get; set; }

        [Parameter("Period Slow MA", Group = "INDICATOR SETTINGS", DefaultValue = 9)]
        public int PeriodSlowMA { get; set; }

        [Parameter("MA Type", Group = "INDICATOR SETTINGS", DefaultValue = MovingAverageType.Weighted)]
        public MovingAverageType MAType { get; set; }

        [Parameter("WPRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 5)]
        public int WPRPeriod { get; set; }

        [Parameter("RSI Period", Group = "INDICATOR SETTINGS", DefaultValue = 14)]
        public int RSIPeriod { get; set; }

        [Parameter("RSI OSLevel", Group = "INDICATOR SETTINGS", DefaultValue = 30)]
        public int OSLevel { get; set; }

        [Parameter("RSI OBLevel", Group = "INDICATOR SETTINGS", DefaultValue = 70)]
        public int OBLevel { get; set; }

        [Parameter("RSI Source", Group = "INDICATOR SETTINGS")]
        public DataSeries RSIAppliedPrice { get; set; }
        #endregion

        #region EA Settings
        [Parameter("Max Slippage ", Group = "EA SETTINGS", DefaultValue = 1, MinValue = 1)]
        public int MaxSlippage { get; set; }

        [Parameter("Max Spread Allowed ", Group = "EA SETTINGS", DefaultValue = 3, MinValue = 1, Step = 0.1)]
        public double MaxSpread { get; set; }

        [Parameter("Bot Label", Group = "EA SETTINGS", DefaultValue = "RH Bot - ")]
        public string BotLabel { get; set; }
        #endregion

        #region Notification Settings

        [Parameter("Popup Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool PopupNotification { get; set; }

        [Parameter("Sound Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool SoundNotification { get; set; }

        [Parameter("Email Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool EmailNotification { get; set; }

        [Parameter("Email address", Group = "NOTIFICATION SETTINGS", DefaultValue = "notify@testmail.com")]
        public string EmailAddress { get; set; }

        [Parameter("Telegram Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool TelegramEnabled { get; set; }

        [Parameter("API Token", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramToken { get; set; }

        [Parameter("Chat IDs (separate by comma)", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramChatIDs { get; set; }
        #endregion

        #region Display Settings
        [Parameter("Show Objects", Group = "DISPLAY SETTINGS", DefaultValue = true)]
        public bool ShowObjects { get; set; }

        [Parameter("FontSize", Group = "DISPLAY SETTINGS", DefaultValue = 12)]
        public int FontSize { get; set; }

        [Parameter("Space to Corner", Group = "DISPLAY SETTINGS", DefaultValue = 10)]
        public int MarginSpace { get; set; }

        [Parameter("Horizontal Alignment", Group = "DISPLAY SETTINGS", DefaultValue = HorizontalAlignment.Left)]
        public HorizontalAlignment PanelHorizontalAlignment { get; set; }

        [Parameter("Vertical Alignment", Group = "DISPLAY SETTINGS", DefaultValue = VerticalAlignment.Top)]
        public VerticalAlignment PanelVerticalAlignment { get; set; }

        [Parameter("Text Color", Group = "DISPLAY SETTINGS", DefaultValue = "Snow")]
        public string ColorText { get; set; }

        [Parameter("Show How To Use", Group = "DISPLAY SETTINGS", DefaultValue = true)]
        public bool ShowHowToUse { get; set; }
        #endregion

        #region Global variables
        private const string HowToUseText = "How to use:\nCtrl + Left Mouse Button - Draw Breakout line\nShift + Left Mouse Button - Draw Retracement line";
        private const string HowToUseObjectName = "LinesTraderText";

        //private SignalLineDrawManager DrawManager { get; set; }
        //private SignalLineRepository SignalLineRepository { get; set; }

        private StackPanel DisplayPanel;
        private TextBlock ShowHeader, ShowADR, ShowCurrentADR, ShowADRPercent, ShowDrawdown, ShowLotsInfo, ShowTradesInfo, ShowTargetInfo, ShowSpread, ShowNextBuy, ShowNextSell, ShowHT;
        private Grid PanelGrid;

        private bool _isPreChecksOk, _isSpreadOK, _isOperatingHours, _isUpSwingLTF, _isUpSwing, _isUpSwingHTF, _switchedToBullish, _switchedToBearish;
        private int  _totalOpenOrders, _totalOpenBuy, _totalOpenSell, _signalEntry, _signalExit;
        private double _gridDistanceBuy, _gridDistanceSell, _atr, _adrCurrent, _adrOverall, _adrPercent, _nextBuyCostAveLevel, _nextSellCostAveLevel,
                        _nextBuyPyAddLevel, _nextSellPyrAddLevel, _PyramidSellStopLoss, _PyramidBuyStopLoss,
                       _highestHighLTF, _lowestHighLTF, _highestLowLTF, _lowestLowLTF, _highestHigh, _lowestHigh, _highestLow, _lowestLow,
                       _highestHighHTF, _lowestHighHTF, _highestLowHTF, _lowestLowHTF, WhenToTrailPrice;
        double[] HTBarHigh, HTBarLow, HTBarClose, HTBarOpen, LTBarHigh, LTBarLow, LTBarClose, LTBarOpen = new double[5];
        int HTOldNumBars = 0, LTOldNumBars = 0;
        bool _isRecoveryTrade, _isPyramidTrade;
        private string OrderComment, _recoverySTR, _pyramidSTR;

        private RelativeStrengthIndex _rsi;
        private WilliamsPctR _williamsPctR;
        private AverageTrueRange _averageTrueRange;
        private MovingAverage _fastMA, _slowMA, _ltffastMA, _ltfslowMA, _htffastMA, _htfslowMA;

        private Bars _dailyBars;
        private Bars _lowerTimeframeBars;
        private Bars _higherTimeframeBars;




        #endregion

        #endregion

        #region Standard event handlers

        #region OnStart function
        protected override void OnStart()
        {
            //For debugging
            System.Diagnostics.Debugger.Launch();

            CheckPreChecks();

            if (!_isPreChecksOk) Stop();

            _dailyBars = MarketData.GetBars(TimeFrame.Daily);
            _lowerTimeframeBars = MarketData.GetBars(LowerTimeframe);
            _higherTimeframeBars = MarketData.GetBars(HigherTimeframe);

            _adrCurrent = 0;
            _adrPercent = 0;
            _adrOverall = 0;

            _williamsPctR = Indicators.WilliamsPctR(WPRPeriod);
            _rsi = Indicators.RelativeStrengthIndex(RSIAppliedPrice, RSIPeriod);
            _averageTrueRange = Indicators.AverageTrueRange(_dailyBars, ADRPeriod, MAType);

            _fastMA = Indicators.MovingAverage(Bars.ClosePrices, PeriodFastMA, MAType);
            _slowMA = Indicators.MovingAverage(Bars.ClosePrices, PeriodSlowMA, MAType);

            _ltffastMA = Indicators.MovingAverage(_lowerTimeframeBars.ClosePrices, PeriodFastMA, MAType);
            _ltfslowMA = Indicators.MovingAverage(_lowerTimeframeBars.ClosePrices, PeriodSlowMA, MAType);

            _htffastMA = Indicators.MovingAverage(MarketData.GetBars(HigherTimeframe).ClosePrices, PeriodFastMA, MAType);
            _htfslowMA = Indicators.MovingAverage(MarketData.GetBars(HigherTimeframe).ClosePrices, PeriodSlowMA, MAType);

            _highestHigh = Bars.HighPrices.Last(2);
            _lowestHigh = Bars.HighPrices.Last(2);
            _highestLow = Bars.LowPrices.Last(2);
            _lowestLow = Bars.LowPrices.Last(2);
            _isUpSwing = false;

            _highestHighLTF = _lowerTimeframeBars.HighPrices.Last(2);
            _lowestHighLTF = _lowerTimeframeBars.HighPrices.Last(2);
            _highestLowLTF = _lowerTimeframeBars.LowPrices.Last(2);
            _lowestLowLTF = _lowerTimeframeBars.LowPrices.Last(2);
            _isUpSwingLTF = false;

            _highestHighHTF = _higherTimeframeBars.HighPrices.Last(2);
            _lowestHighHTF = _higherTimeframeBars.HighPrices.Last(2);
            _highestLowHTF = _higherTimeframeBars.LowPrices.Last(2);
            _lowestLowHTF = _higherTimeframeBars.LowPrices.Last(2);
            _isUpSwingHTF = false;

            _switchedToBullish = false;
            _switchedToBearish = false;

            _recoverySTR = "Recovery";
            _pyramidSTR = "Pyramid";

            OrderComment = BotLabel + MyAutoStrategyName.ToString();

        }
        #endregion

        # region OnTick function
        protected override void OnTick()
        {
            
        }
        #endregion

        # region OnBar function
        protected override void OnBar()
        {
            OnBarInitialization();

            CheckOperationHours();

            CheckSpread();

            ScanOrders();



            EvaluateEntry();


        }
        #endregion

        # region OnStop function
        protected override void OnStop()
        {
            
        }
        #endregion

        # region Exception Function
        protected override void OnException(Exception exception)
        {

        }
        #endregion

        #endregion

        #region Custom Functions 
        #region CheckPreChecks
        private void CheckPreChecks()
        {
            _isPreChecksOk = true;

            //Slippage must be >= 0
            if (MaxSlippage < 0)
            {
                _isPreChecksOk = false;
                Print("Slippage must be a positive value");
                return;
            }
            //MaxSpread must be >= 0
            if (MaxSpread < 0)
            {
                _isPreChecksOk = false;
                Print("Maximum Spread must be a positive value");
                return;
            }
            //MaxRiskPerTrade is a % between 0 and 100
            if (MaxRiskPerTrade < 0 || MaxRiskPerTrade > 100)
            {
                _isPreChecksOk = false;
                Print("Maximum Risk Per Trade must be a percentage between 0 and 100");
                return;
            }
        }

        #endregion

        #region OnBar Initialization 
        private void OnBarInitialization()
        {
            _isPreChecksOk = false;
            _isSpreadOK = false;
            _isOperatingHours = false;

            _totalOpenOrders = 0;
            _totalOpenBuy = 0;
            _totalOpenSell = 0;
            _signalEntry = 0;
            _signalExit = 0;

            _isRecoveryTrade = false;
            _isPyramidTrade = false;
        }
        #endregion

        #region Check Spread
        private void CheckSpread()
        {
            _isSpreadOK = false;
            if (Math.Round(Symbol.Spread / Symbol.PipSize, 2) <= MaxSpread) _isSpreadOK = true;
        }

        #endregion

        #region Check Operation Hours
        private void CheckOperationHours()
        {
            //If we are not using operating hours then IsOperatingHours is true and I skip the other checks
            if (!UseTradingHours)
            {
                _isOperatingHours = true;
                return;
            }

            //Check if the current hour is between the allowed hours of operations, if so IsOperatingHours is set true
            if (TradingHourStart == TradingHourEnd && Server.Time.Hour == TradingHourStart) _isOperatingHours = true;
            if (TradingHourStart < TradingHourEnd && Server.Time.Hour >= TradingHourStart && Server.Time.Hour <= TradingHourEnd) _isOperatingHours = true;
            if (TradingHourStart > TradingHourEnd && ((Server.Time.Hour >= TradingHourStart && Server.Time.Hour <= 23) || (Server.Time.Hour <= TradingHourEnd && Server.Time.Hour >= 0))) _isOperatingHours = true;
        }
        #endregion

        #region Scan Orders
        private void ScanOrders()
        {
            foreach (var position in Positions)
            {
                if (position.SymbolName != SymbolName) continue;
                if (position.Label != OrderComment) continue;
                if (position.TradeType == TradeType.Buy) _totalOpenBuy++;
                if (position.TradeType == TradeType.Sell) _totalOpenSell++;

                _totalOpenOrders++;
            }
        }
        #endregion

        #region Evaluate Entry
        private void EvaluateEntry()
        {
            _signalEntry = 0;
            if (!_isSpreadOK) return;
            if (UseTradingHours && !_isOperatingHours) return;
            if (_totalOpenOrders == MaxPositions) return;

            if (MyTradingMode == TradingMode.Auto || MyTradingMode == TradingMode.Both)
            {
                if (MyAutoStrategyName == AutoStrategyName.Trend_MA)
                {
                    if (_fastMA.Result.LastValue > _slowMA.Result.LastValue &&
                        _williamsPctR.Result.Last(2) < -80 && _williamsPctR.Result.Last(1) > -80)
                        _signalEntry = 1;

                    if (_fastMA.Result.LastValue < _slowMA.Result.LastValue &&
                         _williamsPctR.Result.Last(2) > -20 && _williamsPctR.Result.Last(1) < -20)
                        _signalEntry = -1;
                } 

                if (MyAutoStrategyName == AutoStrategyName.Trend_MA_MTF)
                {
                    if (_fastMA.Result.LastValue    > _slowMA.Result.LastValue    &&
                        _ltffastMA.Result.LastValue > _ltfslowMA.Result.LastValue &&
                        _htffastMA.Result.LastValue > _htfslowMA.Result.LastValue &&
                        _williamsPctR.Result.LastValue > -20)
                        _signalEntry = 1;

                    if (_fastMA.Result.LastValue    < _slowMA.Result.LastValue    &&
                        _ltffastMA.Result.LastValue < _ltfslowMA.Result.LastValue &&
                        _htffastMA.Result.LastValue < _htfslowMA.Result.LastValue &&
                         _williamsPctR.Result.LastValue < -80)
                        _signalEntry = -1;
                }

               /* if (MyAutoStrategyName == AutoStrategyName.HHLL_MTF)
                {
                    if (IsHTFBullish() && IsMTFBullish() && IsLTFBullish() && _switchedToBullish && _williamsPctR.Result.Last(1) > -20) _signalEntry = 1;

                    if (!IsHTFBullish() && !IsMTFBullish() && !IsLTFBullish() && _switchedToBearish && _williamsPctR.Result.Last(1) < -80) _signalEntry = -1;
                }

                if (MyAutoStrategyName == AutoStrategyName.RSIReversion)
                {
                    if (_rsi.Result.Last(1) >= OSLevel && _rsi.Result.Last(2) < OSLevel)
                        _signalEntry = 1;

                    if (_rsi.Result.Last(1) <= OBLevel && _rsi.Result.Last(2) > OBLevel)
                        _signalEntry = -1;
                } */

            }

        }
        #endregion

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

        public const string NAME = "Ultimate Trader Manager";

        public const string VERSION = "1.0";

        #endregion

        #region Enum
        public enum OpenTradeType
        {
            All,
            Buy,
            Sell
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
            HHLL,
            RSIMeanReversion,
            BreakoutTrading
        }

        public enum RiskBase
        {
            BaseEquity,
            BaseBalance,
            BaseMargin, 
            BaseFixedBalance
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
            TP_Auto_RRR,
            TP_Multi
        };

        public enum TrailingMode
        {
            TL_None,
            TL_Fixed,
            TL_Fixed_BE,
            TL_Psar,
            TL_Pyramid,
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

        [Parameter("Open Trade Type", Group = "STRATEGY", DefaultValue = OpenTradeType.All)]
        public OpenTradeType MyOpenTradeType { get; set; }

        [Parameter("Trading Mode", Group = "STRATEGY", DefaultValue = TradingMode.Both)]
        public TradingMode MyTradingMode { get; set; }

        [Parameter("Auto Strategy Name", Group = "STRATEGY", DefaultValue = AutoStrategyName.Trend_MA)]
        public AutoStrategyName MyAutoStrategyName { get; set; }
        #endregion

        #region Trading Hours
        [Parameter("Use Trading Hours", Group = "TRADING HOURS", DefaultValue = true)]
        public bool UseTradingHours { get; set; }

        [Parameter("Starting Hour", Group = "TRADING HOURS", DefaultValue = 02)]
        public int TradingHourStart { get; set; }

        [Parameter("Ending Hour", Group = "TRADING HOURS", DefaultValue = 23)]
        public int TradingHourEnd { get; set; }
        #endregion

        #region Risk Management Settings
        [Parameter("Position size mode", Group = "RISK MANAGEMENT", DefaultValue = PositionSizeMode.Risk_Fixed)]
        public PositionSizeMode MyPositionSizeMode { get; set; }

        [Parameter("Default Lot Size", Group = "RISK MANAGEMENT", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double DefaultLotSize { get; set; }

        [Parameter("Cal Risk From ", Group = "RISK MANAGEMENT", DefaultValue = RiskBase.BaseBalance)]
        public RiskBase MyRiskBase { get; set; }

        [Parameter("Base Fixed Balance ", Group = "RISK MANAGEMENT", DefaultValue = 100)]
        public double MyBaseFixedBalance { get; set; }

        [Parameter("Max Risk % Per Trade", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.1, Step = 0.01)]
        public double MaxRiskPerTrade { get; set; }

        [Parameter("Risk Reward Ratio - 1:", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.1, Step = 0.01)]
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
        
        [Parameter("Use Fake StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = true)]
        public bool UseFakeStopLoss { get; set; }
        
        [Parameter("Fake StopLoss ", Group = "RISK MANAGEMENT", DefaultValue = 200, MinValue = 100, Step = 5)]
        public double FakeStopLoss { get; set; }

        [Parameter("Take Profit Mode", Group = "RISK MANAGEMENT", DefaultValue = TakeProfitMode.TP_Fixed)]
        public TakeProfitMode MyTakeProfitMode { get; set; }

        [Parameter("Default Take Profit", Group = "RISK MANAGEMENT", DefaultValue = 21, MinValue = 5, Step = 1)]
        public double DefaultTakeProfit { get; set; }
        #endregion

        #region Trade Management
        [Parameter("Use Auto Trade Management", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool AutoTradeManagement { get; set; }

        [Parameter("Split Trades", Group = "TRADE MANAGEMENT", DefaultValue = true)]
        public bool SplitTrades { get; set; }

        [Parameter("How Many Split Trades", Group = "TRADE MANAGEMENT", DefaultValue = 2)]
        public double NumOfSplitTrades { get; set; }

        [Parameter("Use Pyramids for TP", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool DoPyramidsTrading { get; set; }

        [Parameter("Stop Bot On Equity Target", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool IsStopOnEquityTarget { get; set; }

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

        [Parameter("Use Trailing Stop ", Group = "TRADE MANAGEMENT", DefaultValue = TrailingMode.TL_Fixed_BE)]
        public TrailingMode MyTrailingMode { get; set; }

        [Parameter("Trail After (Pips) ", Group = "TRADE MANAGEMENT", DefaultValue = 10, MinValue =1)]
        public double WhenToTrail { get; set; }

        [Parameter("Break-Even Losing Trades", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool BreakEvenLosing { get; set; }

        [Parameter("Cost Ave Take Profit", Group = "TRADE MANAGEMENT", DefaultValue = false)]
        public bool IsCostAveTakeProfit { get; set; }

        [Parameter("Commissions pips", Group = "TRADE MANAGEMENT", DefaultValue = 1, MinValue = 1)]
        public int CommissionsPips { get; set; }

        [Parameter("Pending Order Distance (pips)", Group = "TRADE MANAGEMENT", DefaultValue = 2, MinValue = 1)]
        public double PendingOrderDistance { get; set; }

        
        #endregion

        #region  Indicator Settings
        [Parameter("ADRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 10)]
        public int ADRPeriod { get; set; }

        [Parameter("ADR Divisor SL", Group = "INDICATOR SETTINGS", DefaultValue = 3)]
        public double ADR_SL { get; set; }

        [Parameter("WPRPeriod", Group = "INDICATOR SETTINGS", DefaultValue = 5)]
        public int WPRPeriod { get; set; }

        [Parameter("RSI Period", Group = "INDICATOR SETTINGS", DefaultValue = 14)]
        public int RSIPeriod { get; set; }

        [Parameter("RSI OSLevel", Group = "INDICATOR SETTINGS", DefaultValue = 30)]
        public int OSLevel { get; set; }

        [Parameter("RSI OBLevel", Group = "INDICATOR SETTINGS", DefaultValue = 70)]
        public int OBLevel { get; set; }

        [Parameter("Min Acceleration Factor", Group = "Parabolic SAR", DefaultValue = 0.02, MinValue = 0, Step = 0.01)]
        public double MinAccFactor { get; set; }

        [Parameter("Max Acceleration Factor", Group = "Parabolic SAR", DefaultValue = 0.2, MinValue = 0, Step = 0.01)]
        public double MaxAccFactor { get; set; }
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
        [Parameter("LineStyle", Group = "DISPLAY SETTINGS", DefaultValue = LineStyle.Solid)]
        public LineStyle HLineStyle { get; set; }

        [Parameter("Thickness", Group = "DISPLAY SETTINGS", DefaultValue = 1)]
        public int HLineThickness { get; set; }

        [Parameter("Color", Group = "DISPLAY SETTINGS", DefaultValue = "DarkGoldenrod")]
        public string HLineColor { get; set; }

        [Parameter("Transparency", Group = "DISPLAY SETTINGS", DefaultValue = 60, MinValue = 1, MaxValue = 100)]
        public int HLineTransparency { get; set; }

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

        private StackPanel contentPanel;
        private TextBlock ShowHeader, ShowADR, ShowCurrentADR, ShowADRPercent, ShowDrawdown, ShowLotsInfo, ShowTradesInfo, ShowTargetInfo, ShowSpread;
        private Grid PanelGrid;
        private ToggleButton buystoplimitbutton, sellstoplimitbutton;
        private Color hColour;
        private ChartHorizontalLine HorizontalLine;

        private bool _isPreChecksOk, _isSpreadOK, _isOperatingHours, _isUpSwing, _rsiBullishTrigger, _rsiBearishTrigger, buySLbool, sellSLbool, _isRecoveryTrade, _isPyramidTrade;
        private int  _totalOpenOrders, _totalOpenBuy, _totalOpenSell, _totalPendingOrders, _totalPendingBuy, _totalPendingSell, _signalEntry, _signalExit;
        private double _gridDistanceBuy, _gridDistanceSell, _atr, _adrCurrent, _adrOverall, _adrPercent, _nextBuyCostAveLevel, _nextSellCostAveLevel,
                        _nextBuyPyAddLevel, _nextSellPyrAddLevel, _PyramidSellStopLoss, _PyramidBuyStopLoss,
                        _highestHigh, _lowestHigh, _highestLow, _lowestLow, _lastSwingHigh, _lastSwingLow, WhenToTrailPrice;
        double[] HTBarHigh, HTBarLow, HTBarClose, HTBarOpen, LTBarHigh, LTBarLow, LTBarClose, LTBarOpen = new double[5];
        int HTOldNumBars = 0, LTOldNumBars = 0;
        private string OrderComment, _recoverySTR, _pyramidSTR;

        private RelativeStrengthIndex _rsi;
        private WilliamsPctR _williamsPctR;
        private ParabolicSAR parabolicSAR;
        private AverageTrueRange _averageTrueRange;
        private MovingAverage _fastMA, _slowMA, _ltffastMA, _ltfslowMA, _htffastMA, _htfslowMA;

        private Bars _dailyBars;

        #endregion

        #endregion

        #region Standard event handlers

        #region OnStart function
        protected override void OnStart()
        {
            //For debugging
            //System.Diagnostics.Debugger.Launch();

            CheckPreChecks();

            if (!_isPreChecksOk) Stop();

            _dailyBars = MarketData.GetBars(TimeFrame.Daily);

            _adrCurrent = 0;
            _adrPercent = 0;
            _adrOverall = 0;
            _lastSwingHigh = 0.00001; ;
            _lastSwingLow = 1000000;

            _williamsPctR = Indicators.WilliamsPctR(WPRPeriod);
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RSIPeriod);
            _averageTrueRange = Indicators.AverageTrueRange(_dailyBars, ADRPeriod, MovingAverageType.Weighted);
            parabolicSAR = Indicators.ParabolicSAR(MinAccFactor, MaxAccFactor);

            _fastMA = Indicators.MovingAverage(Bars.ClosePrices, 3, MovingAverageType.Weighted);
            _slowMA = Indicators.MovingAverage(Bars.ClosePrices, 9, MovingAverageType.Weighted);

            _ltffastMA = Indicators.MovingAverage(Bars.ClosePrices,15, MovingAverageType.Weighted);
            _ltfslowMA = Indicators.MovingAverage(Bars.ClosePrices, 45, MovingAverageType.Weighted);

            _htffastMA = Indicators.MovingAverage(Bars.ClosePrices, 180, MovingAverageType.Weighted);
            _htfslowMA = Indicators.MovingAverage(Bars.ClosePrices, 540, MovingAverageType.Weighted);

            _highestHigh = Bars.HighPrices.Last(2);
            _lowestHigh = Bars.HighPrices.Last(2);
            _highestLow = Bars.LowPrices.Last(2);
            _lowestLow = Bars.LowPrices.Last(2);

            _rsiBullishTrigger = false;
            _rsiBearishTrigger = false;

            _recoverySTR = "Recovery";
            _pyramidSTR = "Pyramid";

            OrderComment = BotLabel + MyAutoStrategyName.ToString();

            HLineTransparency = (int)(255 * 0.01 * HLineTransparency);
            hColour = Color.FromArgb(HLineTransparency, Color.FromName(HLineColor).R, Color.FromName(HLineColor).G, Color.FromName(HLineColor).B);
            DisplayPanel();
            Chart.MouseMove += OnChartMouseMove;
            Chart.MouseLeave += OnChartMouseLeave;
            Chart.MouseDown += OnChartMouseDown;

        }
        #endregion

        # region OnTick function
        protected override void OnTick()
        {
            var positions = Positions.FindAll(OrderComment);
            double totalUsedLots = positions.Sum(position => position.VolumeInUnits / 100000); // Convert volume to lots
            double totalBotTrades = positions.Count();

            ShowSpread.Text = "Spread  :  " + Math.Round(Symbol.Spread / Symbol.PipSize, 2);
            ShowADR.Text = "ADR  :  " + _adrOverall;
            ShowCurrentADR.Text = "Today Range  :  " + _adrCurrent;
            ShowADRPercent.Text = "Range %  :  " + _adrPercent + "%";
            ShowDrawdown.Text = "DD (Sym) (Acc)  :  " + Account.UnrealizedGrossProfit;
            ShowLotsInfo.Text = "Lots (Sym) (Max)  :  " + totalUsedLots;
            ShowTradesInfo.Text = "Trades (Sym) (Acc)  :  " + totalBotTrades;
            ShowTargetInfo.Text = "Equity Curr -> Targ  :  " + _totalPendingBuy + " -> " + _totalPendingSell;
           // ShowTargetInfo.Text = "Equity Curr -> Targ  :  " + Account.Equity + " -> " + EquityTarget;

            if (AutoTradeManagement)
            {
                Chart.DrawHorizontalLine("ShowNextBuy", _nextBuyCostAveLevel, "#7DDA58", 3, LineStyle.LinesDots);
                Chart.DrawHorizontalLine("ShowNextSell", _nextSellCostAveLevel, "#E4080A", 3, LineStyle.LinesDots);
            }

            if(MyAutoStrategyName == AutoStrategyName.Trend_MA)
            {
                Chart.DrawHorizontalLine("ShowSwingHigh", _lastSwingHigh, "#5335E5", 2, LineStyle.DotsVeryRare);
                Chart.DrawHorizontalLine("ShowSwingLow", _lastSwingLow, "#FC1D85", 1, LineStyle.DotsVeryRare);
            }
            
            CalculateADR();

            CalculateSwingPoints();

            EvaluateExit();

            ExecuteExit();

            ScanOrders();

            ExecuteTrailingStop();

        }
        #endregion

        # region OnBar function
        protected override void OnBar()
        {
            OnBarInitialization();

            CheckOperationHours();

            CheckSpread();

            ScanOrders();

            // Strategy Specific 
            RSIReversion();

            EvaluateEntry();

            ExecuteEntry();

            if (MyAutoStrategyName == AutoStrategyName.BreakoutTrading) IsBullish();

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
            _totalPendingBuy = 0;
            _totalPendingSell = 0;
            _totalPendingOrders = 0;
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

            foreach (var order in PendingOrders)
            {
                if (order.SymbolName != SymbolName) continue;
                if (order.Label != OrderComment) continue;
                if (order.TradeType == TradeType.Buy) _totalPendingBuy++;
                if (order.TradeType == TradeType.Sell) _totalPendingSell++;

                _totalPendingOrders++;
            }

        }
        #endregion

        #region Evaluate Exit
        private void EvaluateExit()
        {
            _signalExit = 0;

            double totalBuyPips = 0;
            double totalSellPips = 0;
            if (_totalOpenBuy > 0)
            {
                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.TradeType != TradeType.Buy) continue;
                    totalBuyPips += position.Pips;
                }
            }

            if (_totalOpenSell > 0)
            {
                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.TradeType != TradeType.Sell) continue;
                    totalSellPips += position.Pips;
                }
            }

            if (BreakEvenLosing)
            {
                if (_totalOpenBuy > 1 && totalBuyPips > _totalOpenBuy) _signalExit = 1;
                if (_totalOpenSell > 1 && totalSellPips > _totalOpenSell) _signalExit = -1;
            }

            if (IsCostAveTakeProfit)
            {
                if (totalBuyPips > DefaultTakeProfit * _totalOpenBuy) _signalExit = 1;
                if (totalSellPips > DefaultTakeProfit * _totalOpenSell) _signalExit = -1;
            }

            if (MyTrailingMode != TrailingMode.TL_Psar)
            {
                if (totalBuyPips > DefaultTakeProfit) _signalExit = 1;
                if (totalSellPips > DefaultTakeProfit) _signalExit = -1;
            }

            if (IsStopOnEquityTarget && Account.Equity > EquityTarget) _signalExit = 2;
        }
        #endregion

        #region Execute Exit
        private void ExecuteExit()
        {

            if (_signalExit == 0) return;

            if (_signalExit == 2)
            {
                foreach (var position in Positions)
                    ClosePositionAsync(position);

                Stop();
            }

            if (_signalExit == 1)
            {
                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.TradeType != TradeType.Buy) continue;
                    ClosePositionAsync(position);
                }

                _nextBuyCostAveLevel = 0;
            }

            if (_signalExit == -1)
            {
                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.TradeType != TradeType.Sell) continue;
                    ClosePositionAsync(position);
                }

                _nextSellCostAveLevel = 0;
            }

        }
        #endregion

        #region Execute Trailing Stop
        private void ExecuteTrailingStop()
        {
            if (MyTrailingMode == TrailingMode.TL_None) return;
            
            if(MyTrailingMode == TrailingMode.TL_Fixed_BE)
            {
                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.Pips < DefaultStopLoss) continue;
                    if (position.TradeType == TradeType.Buy && position.StopLoss == position.EntryPrice + PipsToDigits(1)) continue;
                    if (position.TradeType == TradeType.Sell && position.StopLoss == position.EntryPrice - PipsToDigits(1)) continue;

                    bool isProtected = position.StopLoss.HasValue;
                    double newStopLoss = position.TradeType == TradeType.Buy ? position.EntryPrice + PipsToDigits(1) :  position.EntryPrice - PipsToDigits(1);
                    if (isProtected) ModifyPosition(position, newStopLoss, null);

                }
            }

            if(MyTrailingMode == TrailingMode.TL_Psar)
            {
                double newStopLoss = parabolicSAR.Result.LastValue;

                foreach (var position in Positions)
                {
                    if (position.SymbolName != SymbolName) continue;
                    if (position.Label != OrderComment) continue;
                    if (position.Pips < WhenToTrail) continue;

                    bool isProtected = position.StopLoss.HasValue;

                    if (isProtected) ModifyPosition(position, newStopLoss, null);

                }
            }



        }
        #endregion

        #region Evaluate Entry
        private void EvaluateEntry()
        {
            /****************************  pre checks ****************************/
            _signalEntry = 0;
            if (!_isSpreadOK) return;
            if (UseTradingHours && !_isOperatingHours) return;
            if (_totalOpenOrders == MaxPositions) return;

            /****************************  Automatic Trading ****************************/
            if (MyTradingMode == TradingMode.Auto || MyTradingMode == TradingMode.Both)
            {
                /****************************  Trend MA Strategy ****************************/
                if (MyAutoStrategyName == AutoStrategyName.Trend_MA)
                {
                    bool validBuy = DigitsToPips(Symbol.Ask - _lastSwingLow) < CostAveDistance;
                    bool validSell = DigitsToPips(_lastSwingHigh - Symbol.Bid) < CostAveDistance;
                    if (validBuy &&
                        _williamsPctR.Result.LastValue > -20 &&
                        _fastMA.Result.LastValue > _slowMA.Result.LastValue &&
                        _ltffastMA.Result.LastValue > _ltfslowMA.Result.LastValue &&
                        _htffastMA.Result.LastValue > _htfslowMA.Result.LastValue)
                    {
                        _signalEntry = 1;
                        foreach (var position in Positions.Where(p => p.SymbolName == SymbolName && p.Label == OrderComment && p.TradeType == TradeType.Sell))
                        {
                            if (position.GrossProfit > 0) ClosePosition(position); 
                            else
                            {
                            double newStopLoss = position.TradeType == TradeType.Buy ? position.EntryPrice + PipsToDigits(1) :  position.EntryPrice - PipsToDigits(1);
                            ModifyPosition(position, newStopLoss, null);
                            }

                        }
                    }

                    if (validSell &&
                        _williamsPctR.Result.LastValue < -80 &&
                        _fastMA.Result.LastValue < _slowMA.Result.LastValue &&
                        _ltffastMA.Result.LastValue < _ltfslowMA.Result.LastValue &&
                        _htffastMA.Result.LastValue < _htfslowMA.Result.LastValue)
                    {
                        _signalEntry = -1;
                        foreach (var position in Positions.Where(p => p.SymbolName == SymbolName && p.Label == OrderComment && p.TradeType == TradeType.Buy))
                        {
                            if (position.GrossProfit > 0) ClosePosition(position);
                            else
                            {
                            double newStopLoss = position.TradeType == TradeType.Buy ? position.EntryPrice + PipsToDigits(1) :  position.EntryPrice - PipsToDigits(1);
                            ModifyPosition(position, newStopLoss, null);
                            }
                        }
                    }
                }

                 if (MyAutoStrategyName == AutoStrategyName.HHLL)
                 {
                     if (IsBullish()  && _williamsPctR.Result.Last(1) > -20) _signalEntry = 1;

                     if (!IsBullish() && _williamsPctR.Result.Last(1) < -80) _signalEntry = -1;
                 } 

                if (MyAutoStrategyName == AutoStrategyName.RSIMeanReversion)
                {
                   // if (_rsi.Result.Last(1) >= OSLevel && _rsi.Result.Last(2) < OSLevel)
                   if (_rsiBullishTrigger && Bars.ClosePrices.LastValue > _slowMA.Result.LastValue)
                        _signalEntry = 1;

                    // if (_rsi.Result.Last(1) <= OBLevel && _rsi.Result.Last(2) > OBLevel)
                    if (_rsiBearishTrigger && Bars.ClosePrices.LastValue < _slowMA.Result.LastValue)
                        _signalEntry = -1;
                } 

            }

            if(MyTradingMode == TradingMode.Manual || MyTradingMode == TradingMode.Both) 
            {
            
            }

            if (_signalEntry == 1  && MyOpenTradeType == OpenTradeType.Sell) _signalEntry = 0;
            if (_signalEntry == -1 && MyOpenTradeType == OpenTradeType.Buy) _signalEntry = 0;
        }
        #endregion

        #region Execute Entry
        private void ExecuteEntry()
        {
            if (_signalEntry == 0) return;

            double StopLoss = 0;
            double TakeProfit = 0;
            double _volumeInUnits = 0;

            if (MyStopLossMode == StopLossMode.SL_None) StopLoss = 0;
            if (MyStopLossMode == StopLossMode.SL_Fixed) StopLoss = DefaultStopLoss;
            if (MyStopLossMode == StopLossMode.SL_Auto_ADR) StopLoss = Math.Round(_adrOverall / ADR_SL, 0);

            if (MyTakeProfitMode == TakeProfitMode.TP_None) TakeProfit = 0;
            if (MyTakeProfitMode == TakeProfitMode.TP_Fixed) TakeProfit = DefaultTakeProfit;
            if (MyTakeProfitMode == TakeProfitMode.TP_Auto_RRR) TakeProfit = Math.Round(StopLoss * RiskRewardRatio + CommissionsPips, 0);
            if (MyTakeProfitMode == TakeProfitMode.TP_Auto_ADR) TakeProfit = Math.Round(_adrOverall / StopLoss + CommissionsPips, 0);

            if (MyPositionSizeMode == PositionSizeMode.Risk_Fixed) _volumeInUnits = Symbol.QuantityToVolumeInUnits(DefaultLotSize);
            if (MyPositionSizeMode == PositionSizeMode.Risk_Auto) _volumeInUnits = LotSizeCalculate();
            
            if (UseFakeStopLoss && FakeStopLoss > 2*_adrOverall) StopLoss = FakeStopLoss; 
            if (UseFakeStopLoss && FakeStopLoss < 2*_adrOverall) StopLoss = 2*_adrOverall; 

            if (_signalEntry == 1 && _totalOpenBuy <= MaxPositions && _totalOpenBuy <= MaxBuyPositions)
            {
                if (_totalOpenBuy > 0 && AutoTradeManagement)
                {
                    if (Symbol.Ask < _nextBuyCostAveLevel)
                    {
                        var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, OrderComment, StopLoss, TakeProfit);
                        if (result.Error != null) GetError(result.Error.ToString());
                        else
                        {
                            Print("Position with ID " + result.Position.Id + " was opened");
                            _nextBuyCostAveLevel = Symbol.Ask - PipsToDigits(CostAveDistance);
                            _nextBuyPyAddLevel = Symbol.Ask + PipsToDigits(PyramidDistance);
                        }
                    }

                }

                if (_totalOpenBuy == 0)
                {
                    if (SplitTrades) 
                    { 
                    
                    }
                    var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, OrderComment, StopLoss, TakeProfit);
                    if (result.Error != null) GetError(result.Error.ToString());
                    else
                    {
                        Print("Position with ID " + result.Position.Id + " was opened");
                        _nextBuyCostAveLevel = Symbol.Ask - PipsToDigits(CostAveDistance);
                        _nextBuyPyAddLevel = Symbol.Ask + PipsToDigits(PyramidDistance);
                    }
                }

            }

                if (_signalEntry == -1 && _totalOpenSell <= MaxPositions && _totalOpenSell <= MaxSellPositions)
                {
                    if (_totalOpenSell > 0 && AutoTradeManagement)
                    {
                        if (Symbol.Bid > _nextSellCostAveLevel)
                        {
                            var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, OrderComment, StopLoss, TakeProfit);
                            if (result.Error != null) GetError(result.Error.ToString());
                            else
                            {
                                Print("Position with ID " + result.Position.Id + " was opened");
                                _nextSellCostAveLevel = Symbol.Bid + PipsToDigits(CostAveDistance);
                                _nextSellPyrAddLevel = Symbol.Bid - PipsToDigits(PyramidDistance);
                            }
                        }
                    }

                    if (_totalOpenSell == 0)
                    {
                        var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, OrderComment, StopLoss, TakeProfit);
                        if (result.Error != null) GetError(result.Error.ToString());
                        else
                        {
                            Print("Position with ID " + result.Position.Id + " was opened");
                            _nextSellCostAveLevel = Symbol.Bid + PipsToDigits(CostAveDistance);
                            _nextSellPyrAddLevel = Symbol.Bid - PipsToDigits(PyramidDistance);
                        }
                    }
                }


            
        }
        #endregion

        #region Strategy Specific functions

        #region RSI Reversion Strategy
        private void RSIReversion()
        {
            if (_rsi.Result.LastValue < OSLevel)
            {
                _rsiBullishTrigger = true;
                _rsiBearishTrigger = false;
            }
            else if (_rsi.Result.LastValue > OBLevel)
            {
                _rsiBullishTrigger = false;
                _rsiBearishTrigger = true;
            }
            else 
            {
                 _rsiBearishTrigger = false;
                 _rsiBullishTrigger = false;
            }
        }
        #endregion

        #region Current Bullish
        private bool IsBullish()
        {
            double highPrice = Bars.HighPrices.Last(1);
            double lowPrice = Bars.LowPrices.Last(1);

            if (_isUpSwing)
            {
                if (lowPrice > _highestLow)
                {
                    _highestLow = lowPrice;
                    _highestHigh = Math.Max(highPrice, _highestHigh);
                    if (MyAutoStrategyName == AutoStrategyName.BreakoutTrading) Chart.DrawHorizontalLine("ShowSwingHigh", _highestHigh, "#5335E5", 2, LineStyle.DotsVeryRare);
                }
                if (highPrice < _highestLow)
                {
                    _isUpSwing = false;
                    _lowestHigh = highPrice;
                    _lastSwingHigh = _highestHigh;
                    _lowestLow = double.MaxValue;

                    if (MyAutoStrategyName == AutoStrategyName.BreakoutTrading)
                    {
                        double openprice = _highestHigh + Symbol.Spread + PipsToDigits(PendingOrderDistance);

                        /*
                        if (_totalPendingBuy > 0)
                        foreach (var order in PendingOrders.Where(p => p.SymbolName == SymbolName && p.TradeType == TradeType.Buy && p.Label == OrderComment))
                            order.ModifyTargetPrice(openprice);

                        else
                        PlaceStopOrderAsync(TradeType.Buy, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, DefaultStopLoss, DefaultTakeProfit);
                        */
                        if (PendingOrders.Count(p => p.SymbolName == SymbolName && p.Label == OrderComment && p.OrderType == PendingOrderType.Stop && p.TradeType == TradeType.Buy) == 0)
                        //    foreach (var order in PendingOrders.Where(p => p.TradeType == TradeType.Buy))
                          //      order.ModifyTargetPrice(openprice);
                                //order.Cancel();
                      //  else
                            PlaceStopOrderAsync(TradeType.Buy, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, DefaultStopLoss, DefaultTakeProfit);

                    }
                }
            }
            else
            {
                if (highPrice < _lowestHigh)
                {
                    _lowestHigh = highPrice;
                    _lowestLow = Math.Min(lowPrice, _lowestLow);
                    if (MyAutoStrategyName == AutoStrategyName.BreakoutTrading)  Chart.DrawHorizontalLine("ShowSwingLow", _lowestLow, "#FC1D85", 1, LineStyle.DotsVeryRare);
                }
                if (lowPrice > _lowestHigh)
                {
                    _isUpSwing = true;
                    _highestLow = lowPrice;
                    _lastSwingLow = _lowestLow;
                    _highestHigh = double.MinValue;

                    if (MyAutoStrategyName == AutoStrategyName.BreakoutTrading)
                    {
                        double openprice = _lowestLow - Symbol.Spread - PipsToDigits(PendingOrderDistance);

                        /*
                        if (_totalPendingSell > 0)
                            foreach (var order in PendingOrders.Where(p => p.SymbolName == SymbolName && p.TradeType == TradeType.Sell && p.Label == OrderComment))
                                order.ModifyTargetPrice(openprice);

                        else
                            PlaceStopOrderAsync(TradeType.Sell, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, DefaultStopLoss, DefaultTakeProfit); 
                        */

                        if (PendingOrders.Count(p => p.SymbolName == SymbolName && p.Label == OrderComment && p.OrderType == PendingOrderType.Stop && p.TradeType == TradeType.Sell) == 0)
                        //    foreach (var order in PendingOrders.Where(p => p.TradeType == TradeType.Sell))
                          //       order.ModifyTargetPrice(openprice);
                                //order.Cancel();
                       // else
                            PlaceStopOrderAsync(TradeType.Sell, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, DefaultStopLoss, DefaultTakeProfit);

                    }
                }
            }
            return _isUpSwing;
        }
        #endregion

        

       
 
        #endregion

        #region Helper Functions
        #region Lot Size Calculate
        private double LotSizeCalculate()
        {
            double RiskBaseAmount = 0;
            double _lotSize = DefaultLotSize;
            if (MyRiskBase == RiskBase.BaseEquity) RiskBaseAmount = Account.Equity;
            if (MyRiskBase == RiskBase.BaseBalance) RiskBaseAmount = Account.Balance;
            if (MyRiskBase == RiskBase.BaseMargin) RiskBaseAmount = Account.FreeMargin;
            if (MyRiskBase == RiskBase.BaseFixedBalance) RiskBaseAmount = MyBaseFixedBalance;

            if (MyStopLossMode == StopLossMode.SL_Auto_ADR)
            {
                double moneyrisk = RiskBaseAmount * (MaxRiskPerTrade / 100);
                double sl_double = Math.Round(_adrOverall / ADR_SL, 0) * Symbol.PipSize;
                _lotSize = Math.Round(Symbol.VolumeInUnitsToQuantity(moneyrisk / ((sl_double * Symbol.TickValue) / Symbol.TickSize)), 2);
            }

            if (MyStopLossMode == StopLossMode.SL_Fixed || MyStopLossMode == StopLossMode.SL_None)
            {
                double moneyrisk = RiskBaseAmount * (MaxRiskPerTrade / 100);
                double sl_double = DefaultStopLoss * Symbol.PipSize;
                _lotSize = Math.Round(Symbol.VolumeInUnitsToQuantity(moneyrisk / ((sl_double * Symbol.TickValue) / Symbol.TickSize)), 2);
                _lotSize = Symbol.QuantityToVolumeInUnits(_lotSize);
            }

            if (_lotSize < Symbol.VolumeInUnitsMin)
                return Symbol.VolumeInUnitsMin;

            return _lotSize;
        }
        #endregion

        #region ADR Calculations
        private void CalculateADR()
        {
            double sum = 0;

            for (int i = 1; i <= ADRPeriod; i++)
            {
                double dailyRange = (_dailyBars.HighPrices.Last(i) - _dailyBars.LowPrices.Last(i)) / Symbol.PipSize;
                sum += dailyRange;
            }

            _adrOverall = Math.Round(sum / ADRPeriod, 0);
            _adrCurrent = Math.Round((_dailyBars.HighPrices.LastValue - _dailyBars.LowPrices.LastValue) / Symbol.PipSize, 0);
            _adrPercent = Math.Round((1 - Math.Abs(_adrCurrent - _adrOverall) / _adrOverall) * 100, 0);
        }
        #endregion

        #region Swing Points Calculations
        private void CalculateSwingPoints()
        {
            double highPriceLTF = Bars.HighPrices.LastValue;
            double lowPriceLTF = Bars.LowPrices.LastValue;

            if (_ltffastMA.Result.LastValue > _ltfslowMA.Result.LastValue)
            {
                if (_lastSwingHigh < highPriceLTF)                         _lastSwingHigh = highPriceLTF;
            }

            if (_ltffastMA.Result.LastValue < _ltfslowMA.Result.LastValue)
            {
                if (_lastSwingLow > lowPriceLTF)                           _lastSwingLow = lowPriceLTF;
            }
        }
        #endregion

        #region Digits To Pips
        public double DigitsToPips(double _digits)
        {
            return Math.Round(_digits / Symbol.PipSize, 1);
        }
        #endregion

        #region Pips To Digits
        public double PipsToDigits(double _pips)
        {
            return Math.Round(_pips * Symbol.PipSize, Symbol.Digits);
        }
        #endregion

        #region Real Spread
        public double RealSpread()
        {
            return Math.Round(Symbol.Spread / Symbol.PipSize, 2);
        }
        #endregion

        #region Errors
        private void GetError(string error)
        {
            //  Print the error to the log
            switch (error)
            {
                case "ErrorCode.BadVolume":
                    Print("Invalid Volume amount");
                    break;
                case "ErrorCode.TechnicalError":
                    Print("Error. Confirm that the trade command parameters are valid");
                    break;
                case "ErrorCode.NoMoney":
                    Print("Not enough money to trade.");
                    break;
                case "ErrorCode.Disconnected":
                    Print("The server is disconnected.");
                    break;
                case "ErrorCode.MarketClosed":
                    Print("The market is closed.");
                    break;
                case "ErrorCode.EntityNotFound":
                    Print("Position not found");
                    break;
                case "ErrorCode.Timeout":
                    Print("Operation timed out");
                    break;
                case "ErrorCode.UnknownSymbol":
                    Print("Unknown symbol.");
                    break;
                case "ErrorCode.InvalidStopLossTakeProfit":
                    Print("The invalid Stop Loss or Take Profit.");
                    break;
                case "ErrorCode.InvalidRequest":
                    Print("The invalid request.");
                    break;
            }
        }
        #endregion

        #endregion

        #region Display Functions 
        private void DisplayPanel()
        {


            contentPanel = new StackPanel
            {

                HorizontalAlignment = PanelHorizontalAlignment,
                VerticalAlignment = PanelVerticalAlignment,
                Width = 225,
                Style = Styles.ContentStyle()
            };

            var grid = new Grid(33, 2);

            ShowHeader = new TextBlock
            {
                Text = "Trading Panel",
                Style = Styles.CreateHeaderStyle()
            };

            ShowSpread = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowADR = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowCurrentADR = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowADRPercent = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowDrawdown = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowLotsInfo = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowTradesInfo = new TextBlock { Style = Styles.TextBodyStyle() };
            ShowTargetInfo = new TextBlock { Style = Styles.TextBodyStyle() };
            

            grid.Columns[1].SetWidthInPixels(3);
            buystoplimitbutton = new ToggleButton
            {
                Text = "BUY - Stop/Limit",
                Style = Styles.BuyButtonStyle()
            };

            sellstoplimitbutton = new ToggleButton
            {
                Text = "SELL - Stop/Limit",
                Style = Styles.SellButtonStyle()
            };

            grid.AddChild(ShowHeader, 0, 0);
            grid.AddChild(ShowSpread, 1, 0);
            grid.AddChild(ShowADR, 2, 0);
            grid.AddChild(ShowCurrentADR, 3, 0);
            grid.AddChild(ShowADRPercent, 4, 0);
            grid.AddChild(ShowDrawdown, 5, 0);
            grid.AddChild(ShowLotsInfo, 6, 0);
            grid.AddChild(ShowTradesInfo, 7, 0);
            grid.AddChild(ShowTargetInfo, 8, 0);
            grid.AddChild(buystoplimitbutton, 9, 0, 1, 2);
            grid.AddChild(sellstoplimitbutton, 10, 0, 1, 2);



            buystoplimitbutton.Click += buySLbutton;
            sellstoplimitbutton.Click += SellSLbutton;
            contentPanel.AddChild(grid);
            Chart.AddControl(contentPanel);
        }
        private void buySLbutton(ToggleButtonEventArgs e)
        {
            if (buystoplimitbutton.IsChecked == true) buySLbool = true;
            else buySLbool = false;
        }

        private void SellSLbutton(ToggleButtonEventArgs e)
        {
            if (sellstoplimitbutton.IsChecked == true) sellSLbool = true;
            else sellSLbool = false;
        }
        private void buystoplimitorder(double openprice)
        {
            openprice = openprice + Symbol.Spread + PipsToDigits(PendingOrderDistance);
            if (openprice <= Symbol.Ask) PlaceLimitOrderAsync(TradeType.Buy, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, FakeStopLoss, DefaultTakeProfit);
            if (openprice > Symbol.Ask) PlaceStopOrderAsync(TradeType.Buy, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, FakeStopLoss,DefaultTakeProfit);
        }

        private void sellstoplimitorder(double openprice)
        {
            openprice = openprice - Symbol.Spread - PipsToDigits(PendingOrderDistance);
            if (openprice >= Symbol.Bid) PlaceLimitOrderAsync(TradeType.Sell, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, FakeStopLoss, DefaultTakeProfit);
            if (openprice < Symbol.Bid) PlaceStopOrderAsync(TradeType.Sell, SymbolName, Symbol.QuantityToVolumeInUnits(DefaultLotSize), openprice, OrderComment, FakeStopLoss, DefaultTakeProfit);
        }

        private void OnChartMouseDown(ChartMouseEventArgs obj)
        {
            if (buySLbool == true)
            {
                buystoplimitorder(Math.Round(obj.YValue, Symbol.Digits));
                buystoplimitbutton.IsChecked = false;
                buySLbool = false;
            }

            if (sellSLbool == true)
            {
                sellstoplimitorder(Math.Round(obj.YValue, Symbol.Digits));
                sellstoplimitbutton.IsChecked = false;
                sellSLbool = false;
            }

            Chart.RemoveObject("HorizontalLine");
            Chart.RemoveObject("price");
        }

        private void OnChartMouseMove(ChartMouseEventArgs obj)
        {
            if (buySLbool == true)
            {
                if (buySLbool == true)
                {
                    HorizontalLine = Chart.DrawHorizontalLine("stoplimitHorizontalLine", obj.YValue, Color.FromHex("#2C820A"), HLineThickness, HLineStyle);
                    if (Math.Round(obj.YValue, Symbol.Digits) <= Symbol.Ask)
                    {
                        var sprice = Chart.DrawText("stoplimitprice", "Buy Limit " + Math.Round(obj.YValue, Symbol.Digits).ToString(), Chart.FirstVisibleBarIndex, obj.YValue, Color.FromHex("#2C820A"));
                    }
                    else if (Math.Round(obj.YValue, Symbol.Digits) > Symbol.Ask)
                    {
                        var sprice = Chart.DrawText("stoplimitprice", "Buy Stop " + Math.Round(obj.YValue, Symbol.Digits).ToString(), Chart.FirstVisibleBarIndex, obj.YValue, Color.FromHex("#2C820A"));
                    }
                }
            }

            if (sellSLbool == true)
            {
                if (sellSLbool == true)
                {
                    HorizontalLine = Chart.DrawHorizontalLine("stoplimitHorizontalLine", obj.YValue, Color.FromHex("#F05824"), HLineThickness, HLineStyle);
                    if (Math.Round(obj.YValue, Symbol.Digits) >= Symbol.Bid)
                    {
                        var sprice = Chart.DrawText("stoplimitprice", "Sell Limit " + Math.Round(obj.YValue, Symbol.Digits).ToString(), Chart.FirstVisibleBarIndex, obj.YValue, Color.FromHex("#F05824"));
                    }
                    else if (Math.Round(obj.YValue, Symbol.Digits) < Symbol.Bid)
                    {
                        var sprice = Chart.DrawText("stoplimitprice", "Sell Stop " + Math.Round(obj.YValue, Symbol.Digits).ToString(), Chart.FirstVisibleBarIndex, obj.YValue, Color.FromHex("#F05824"));
                    }
                }
            }
        }
        void OnChartMouseLeave(ChartMouseEventArgs obj)
        {
            Chart.RemoveObject("stoplimitHorizontalLine");
            Chart.RemoveObject("stoplimitprice");
        }

        private static Color GetColorWithOpacity(Color baseColor, decimal opacity)
        {
            var alpha = (int)Math.Round(byte.MaxValue * opacity, MidpointRounding.AwayFromZero);
            return Color.FromArgb(alpha, baseColor);
        }



        #endregion

        #endregion
    }

    #region Helper Classes
    public static class Styles
    {
        public static Style CreateHeaderStyle()
        {
            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, GetColorWithOpacity("#FFFFFF", 0.70m), ControlState.DarkTheme);
            style.Set(ControlProperty.ForegroundColor, GetColorWithOpacity("#000000", 0.65m), ControlState.LightTheme);
            style.Set(ControlProperty.Margin, 5);
            style.Set(ControlProperty.FontSize, 15);
            style.Set(ControlProperty.FontStyle, FontStyle.Oblique);
            return style;
        }

        public static Style TextBodyStyle()
        {
            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, GetColorWithOpacity("#FFFFFF", 0.70m), ControlState.DarkTheme);
            style.Set(ControlProperty.ForegroundColor, GetColorWithOpacity("#000000", 0.65m), ControlState.LightTheme);
            style.Set(ControlProperty.Margin, 5);
            style.Set(ControlProperty.FontFamily, "Cambria");
            style.Set(ControlProperty.FontSize, 12);
            return style;
        }

        public static Style ContentStyle()
        {
            var contetstyle = new Style();
            contetstyle.Set(ControlProperty.CornerRadius, 3);
            contetstyle.Set(ControlProperty.BackgroundColor, GetColorWithOpacity(Color.FromHex("#292929"), 0.85m), ControlState.DarkTheme);
            contetstyle.Set(ControlProperty.BackgroundColor, GetColorWithOpacity(Color.FromHex("#FFFFFF"), 0.85m), ControlState.LightTheme);
            contetstyle.Set(ControlProperty.Margin, "20 20 20 20");
            return contetstyle;
        }

        public static Style BuyButtonStyle()
        {
            var buystoplimitstyle = new Style();
            buystoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#2C820A"), ControlState.DarkTheme);
            buystoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#2C820A"), ControlState.LightTheme);
            buystoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#20570A"), ControlState.DarkTheme | ControlState.Checked);
            buystoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#20570A"), ControlState.LightTheme | ControlState.Checked);
            buystoplimitstyle.Set(ControlProperty.ForegroundColor, Color.FromHex("#FFFFFF"), ControlState.DarkTheme);
            buystoplimitstyle.Set(ControlProperty.ForegroundColor, Color.FromHex("#FFFFFF"), ControlState.LightTheme);
            buystoplimitstyle.Set(ControlProperty.Margin, 3);
            return buystoplimitstyle;
        }

        public static Style SellButtonStyle()
        {
            var sellstoplimitstyle = new Style();
            sellstoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#F05824"), ControlState.DarkTheme);
            sellstoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#F05824"), ControlState.LightTheme);
            sellstoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#8B0000"), ControlState.DarkTheme | ControlState.Checked);
            sellstoplimitstyle.Set(ControlProperty.BackgroundColor, Color.FromHex("#8B0000"), ControlState.LightTheme | ControlState.Checked);
            sellstoplimitstyle.Set(ControlProperty.ForegroundColor, Color.FromHex("#FFFFFF"), ControlState.DarkTheme);
            sellstoplimitstyle.Set(ControlProperty.ForegroundColor, Color.FromHex("#FFFFFF"), ControlState.LightTheme);
            sellstoplimitstyle.Set(ControlProperty.Margin, 3);
            return sellstoplimitstyle;
        }
        private static Color GetColorWithOpacity(Color baseColor, decimal opacity)
        {
            var alpha = (int)Math.Round(byte.MaxValue * opacity, MidpointRounding.AwayFromZero);
            return Color.FromArgb(alpha, baseColor);
        }
    }
    #endregion
}
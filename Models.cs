// Models.cs — core data structures for the portfolio engine

using System;
using System.Collections.Generic;

namespace PortfolioEngine.Models
{
    // represents a single open position
    public class Position
    {
        public string Symbol { get; set; }
        public string Sector { get; set; } = "Unknown";
        public int Quantity { get; set; }
        public decimal AvgBuyPrice { get; set; }   // weighted average after position averaging
        public decimal CurrentPrice { get; set; }
        public decimal StopLoss { get; set; }       // optional stop-loss level
        public DateTime OpenDate { get; set; }

        // unrealized p/l on open position
        public decimal UnrealizedPnL => (CurrentPrice - AvgBuyPrice) * Quantity;

        public decimal UnrealizedPnLPercent => AvgBuyPrice > 0
            ? Math.Round((CurrentPrice - AvgBuyPrice) / AvgBuyPrice * 100, 2)
            : 0;

        public decimal TotalCost => AvgBuyPrice * Quantity;
        public decimal CurrentValue => CurrentPrice * Quantity;

        // weight in portfolio — set externally by AnalyticsService
        public double PortfolioWeight { get; set; }
    }

    // single closed trade record used for realized p/l and performance stats
    public class ClosedTrade
    {
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }

        // realized profit or loss on this trade
        public decimal RealizedPnL => (SellPrice - BuyPrice) * Quantity;
        public bool IsWin => RealizedPnL > 0;
    }

    // transaction log entry
    public class Transaction
    {
        public string Type { get; set; }       // BUY / SELL / UPDATE / TRANSFER
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; }
    }

    // user account — holds positions, history and realized p/l
    public class User
    {
        public string Name { get; set; }
        public decimal Balance { get; set; } = 10000m;   // starting capital for risk calc
        public decimal RealizedPnL { get; set; } = 0m;   // cumulative realized p/l
        public List<Position> Positions { get; set; } = new();
        public List<ClosedTrade> ClosedTrades { get; set; } = new();
        public List<Transaction> History { get; set; } = new();

        // total unrealized p/l across all open positions
        public decimal UnrealizedPnL => Positions.Count > 0
            ? Positions.Sum(p => p.UnrealizedPnL)
            : 0;

        // total p/l = realized + unrealized
        public decimal TotalPnL => RealizedPnL + UnrealizedPnL;

        // total capital deployed in open positions
        public decimal TotalExposure => Positions.Count > 0
            ? Positions.Sum(p => p.CurrentValue)
            : 0;
    }

    // result of a risk calculation
    public class RiskResult
    {
        public decimal RiskAmount { get; set; }          // dollar risk on trade
        public decimal PositionSize { get; set; }        // recommended position size (shares)
        public decimal ExposurePercent { get; set; }     // % of portfolio in this trade
        public string Warning { get; set; }              // any risk warnings
    }

    // portfolio performance snapshot
    public class PerformanceStats
    {
        public int TotalTrades { get; set; }
        public int WinCount { get; set; }
        public int LossCount { get; set; }
        public double WinRate { get; set; }
        public decimal AvgWin { get; set; }
        public decimal AvgLoss { get; set; }
        public decimal RiskRewardRatio { get; set; }
        public decimal Expectancy { get; set; }          // (WinRate * AvgWin) - (LossRate * AvgLoss)
        public decimal ProfitFactor { get; set; }        // gross profit / gross loss
        public decimal TotalRealizedPnL { get; set; }
    }
}

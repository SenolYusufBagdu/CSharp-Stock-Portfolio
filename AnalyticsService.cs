// AnalyticsService.cs — win rate, expectancy, profit factor, portfolio heatmap

using System;
using System.Linq;
using PortfolioEngine.Models;

namespace PortfolioEngine.Services
{
    public class AnalyticsService
    {
        // calculate performance stats from closed trades
        public PerformanceStats GetStats(User user)
        {
            var trades = user.ClosedTrades;
            var stats  = new PerformanceStats();

            if (!trades.Any()) return stats;

            stats.TotalTrades      = trades.Count;
            stats.WinCount         = trades.Count(t => t.IsWin);
            stats.LossCount        = trades.Count(t => !t.IsWin);
            stats.WinRate          = Math.Round((double)stats.WinCount / stats.TotalTrades * 100, 2);
            stats.TotalRealizedPnL = trades.Sum(t => t.RealizedPnL);

            var wins   = trades.Where(t => t.IsWin).ToList();
            var losses = trades.Where(t => !t.IsWin).ToList();

            stats.AvgWin  = wins.Any()   ? Math.Round(wins.Average(t => t.RealizedPnL), 2)          : 0;
            stats.AvgLoss = losses.Any() ? Math.Round(Math.Abs(losses.Average(t => t.RealizedPnL)), 2) : 0;

            // risk/reward ratio
            stats.RiskRewardRatio = stats.AvgLoss > 0
                ? Math.Round(stats.AvgWin / stats.AvgLoss, 2)
                : 0;

            // expectancy = (WinRate * AvgWin) - (LossRate * AvgLoss)
            double winRate  = stats.WinCount  / (double)stats.TotalTrades;
            double lossRate = stats.LossCount / (double)stats.TotalTrades;
            stats.Expectancy = Math.Round(
                (decimal)winRate * stats.AvgWin - (decimal)lossRate * stats.AvgLoss, 2);

            // profit factor = gross profit / gross loss
            decimal grossProfit = wins.Sum(t => t.RealizedPnL);
            decimal grossLoss   = Math.Abs(losses.Sum(t => t.RealizedPnL));
            stats.ProfitFactor  = grossLoss > 0
                ? Math.Round(grossProfit / grossLoss, 2)
                : 0;

            return stats;
        }

        // print performance stats to console
        public void PrintStats(User user)
        {
            var s = GetStats(user);
            Console.WriteLine();
            Console.WriteLine("  ── Performance Analytics ─────────────────");
            Console.WriteLine($"  Total Trades     : {s.TotalTrades}");
            Console.WriteLine($"  Wins / Losses    : {s.WinCount} / {s.LossCount}");
            Console.WriteLine($"  Win Rate         : {s.WinRate}%");
            Console.WriteLine($"  Avg Win          : {s.AvgWin:N2}");
            Console.WriteLine($"  Avg Loss         : {s.AvgLoss:N2}");
            Console.WriteLine($"  Risk/Reward      : 1 : {s.RiskRewardRatio}");
            Console.WriteLine($"  Expectancy       : {s.Expectancy:+N2;-N2} per trade");
            Console.WriteLine($"  Profit Factor    : {s.ProfitFactor}");
            Console.WriteLine($"  Total Realized   : {s.TotalRealizedPnL:+N2;-N2}");
        }

        // portfolio heatmap — sector breakdown and position weights
        public void PrintHeatmap(User user)
        {
            if (!user.Positions.Any())
            {
                Console.WriteLine("  No open positions.");
                return;
            }

            decimal total = user.Positions.Sum(p => p.CurrentValue);

            // update portfolio weights
            foreach (var p in user.Positions)
                p.PortfolioWeight = total > 0
                    ? Math.Round((double)(p.CurrentValue / total * 100), 2)
                    : 0;

            Console.WriteLine();
            Console.WriteLine("  ── Portfolio Heatmap ─────────────────────");
            Console.WriteLine($"  {"Symbol",-8} {"Sector",-14} {"Qty",6} {"Avg Cost",10} {"Current",10} {"Value",12} {"Weight",8} {"P/L",12} {"P/L%",8}");
            Console.WriteLine(new string('─', 95));

            // sort by portfolio weight descending
            foreach (var p in user.Positions.OrderByDescending(p => p.PortfolioWeight))
            {
                string arrow = p.UnrealizedPnL >= 0 ? "▲" : "▼";
                Console.ForegroundColor = p.UnrealizedPnL >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"  {p.Symbol,-8} {p.Sector,-14} {p.Quantity,6} {p.AvgBuyPrice,10:N2} " +
                                  $"{p.CurrentPrice,10:N2} {p.CurrentValue,12:N2} {p.PortfolioWeight,7:0.0}% " +
                                  $"{arrow}{Math.Abs(p.UnrealizedPnL),11:N2} {p.UnrealizedPnLPercent,7:+0.00;-0.00}%");
                Console.ResetColor();
            }

            Console.WriteLine(new string('─', 95));
            decimal totalPnL = user.Positions.Sum(p => p.UnrealizedPnL);
            Console.WriteLine($"  {"TOTAL",-8} {"",14} {"",6} {"",10} {"",10} {total,12:N2} {"100%",8} " +
                              $"{(totalPnL >= 0 ? "▲" : "▼")}{Math.Abs(totalPnL),11:N2}");

            // sector concentration
            Console.WriteLine();
            Console.WriteLine("  ── Sector Breakdown ──────────────────────");
            var sectors = user.Positions
                .GroupBy(p => p.Sector)
                .Select(g => new {
                    Sector = g.Key,
                    Value  = g.Sum(p => p.CurrentValue),
                    Weight = Math.Round((double)(g.Sum(p => p.CurrentValue) / total * 100), 1)
                })
                .OrderByDescending(s => s.Weight);

            foreach (var s in sectors)
                Console.WriteLine($"  {s.Sector,-16} {s.Value,12:N2}  ({s.Weight}%)");
        }

        // p/l summary — realized vs unrealized
        public void PrintPnLSummary(User user)
        {
            Console.WriteLine();
            Console.WriteLine("  ── P/L Summary ───────────────────────────");
            Console.WriteLine($"  Realized P/L     : {user.RealizedPnL:+N2;-N2}");
            Console.WriteLine($"  Unrealized P/L   : {user.UnrealizedPnL:+N2;-N2}");
            Console.WriteLine($"  Total P/L        : {user.TotalPnL:+N2;-N2}");
        }
    }
}

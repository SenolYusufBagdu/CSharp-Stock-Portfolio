// RiskService.cs — position sizing, exposure tracking, stop-loss management

using System;
using System.Linq;
using PortfolioEngine.Models;

namespace PortfolioEngine.Services
{
    public class RiskService
    {
        // default max risk per trade as % of balance
        private const double DefaultMaxRiskPercent = 1.0;

        // calculate recommended position size based on risk parameters
        // formula: positionSize = (balance * riskPercent) / (entryPrice - stopLoss)
        public RiskResult CalculatePositionSize(
            decimal balance,
            decimal entryPrice,
            decimal stopLoss,
            double riskPercent = DefaultMaxRiskPercent)
        {
            var result = new RiskResult();

            if (entryPrice <= 0 || stopLoss <= 0 || stopLoss >= entryPrice)
            {
                result.Warning = "Invalid entry or stop-loss price.";
                return result;
            }

            decimal risk = balance * (decimal)(riskPercent / 100.0);
            decimal riskPerShare = entryPrice - stopLoss;

            result.RiskAmount    = Math.Round(risk, 2);
            result.PositionSize  = Math.Floor(risk / riskPerShare);
            result.ExposurePercent = entryPrice > 0
                ? Math.Round(result.PositionSize * entryPrice / balance * 100, 2)
                : 0;

            // warn if position is too concentrated
            if (result.ExposurePercent > 20)
                result.Warning = $"⚠ High concentration: {result.ExposurePercent}% of portfolio in one trade.";

            return result;
        }

        // check if any open position has breached its stop-loss
        public void CheckStopLosses(User user)
        {
            foreach (var p in user.Positions)
            {
                if (p.StopLoss > 0 && p.CurrentPrice <= p.StopLoss)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ⚠ STOP-LOSS TRIGGERED: {p.Symbol} " +
                                      $"| Current: {p.CurrentPrice:N2} | SL: {p.StopLoss:N2}");
                    Console.ResetColor();
                }
            }
        }

        // total % of balance currently deployed in open positions
        public decimal GetTotalExposurePercent(User user)
        {
            if (user.Balance <= 0 || !user.Positions.Any()) return 0;
            decimal exposure = user.Positions.Sum(p => p.CurrentValue);
            return Math.Round(exposure / user.Balance * 100, 2);
        }

        // portfolio-level risk summary
        public void PrintRiskSummary(User user)
        {
            Console.WriteLine();
            Console.WriteLine("  ── Risk Summary ──────────────────────────");
            Console.WriteLine($"  Balance           : {user.Balance,12:N2}");
            Console.WriteLine($"  Total Exposure    : {user.TotalExposure,12:N2}  " +
                              $"({GetTotalExposurePercent(user)}% of balance)");

            // largest single position
            if (user.Positions.Any())
            {
                var top = user.Positions.OrderByDescending(p => p.CurrentValue).First();
                decimal topWeight = Math.Round(top.CurrentValue / user.TotalExposure * 100, 1);
                Console.WriteLine($"  Largest Position  : {top.Symbol,-8}  {topWeight}% of portfolio");
            }

            // positions near stop-loss (within 2%)
            foreach (var p in user.Positions.Where(p => p.StopLoss > 0))
            {
                decimal dist = Math.Round((p.CurrentPrice - p.StopLoss) / p.CurrentPrice * 100, 2);
                if (dist < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ {p.Symbol} is {dist}% above stop-loss ({p.StopLoss:N2})");
                    Console.ResetColor();
                }
            }
        }
    }
}

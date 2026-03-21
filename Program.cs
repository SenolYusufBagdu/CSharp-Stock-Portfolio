

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortfolioEngine.Models;
using PortfolioEngine.Repository;
using PortfolioEngine.Services;

namespace PortfolioEngine
{
    class Program
    {
        // exposed for PortfolioService.GetAllUsers
        // exposed 
        public static List<User> Users = new();

        static readonly UserRepository     _repo      = new();
        static readonly PortfolioService   _portfolio = new(_repo);
        static readonly RiskService        _risk      = new();
        static readonly AnalyticsService   _analytics = new();
        static          IMarketDataProvider _market   = new ManualPriceProvider();

        static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Users = _repo.LoadAll();

            while (true)
            {
                Console.Clear();
                Header("QUANTITATIVE PORTFOLIO ENGINE");
                Console.WriteLine("  1  →  Select User");
                Console.WriteLine("  2  →  New User");
                Console.WriteLine("  3  →  All Users Overview");
                Console.WriteLine("  4  →  Search by Symbol");
                Console.WriteLine("  5  →  Set Market Data Provider");
                Console.WriteLine("  0  →  Exit");
                Divider();
                Console.Write("Choice: ");
                string c = Console.ReadLine();

                if      (c == "1") await SelectUser();
                else if (c == "2") NewUser();
                else if (c == "3") AllUsersOverview();
                else if (c == "4") SearchBySymbol();
                else if (c == "5") SetProvider();
                else if (c == "0") break;
            }
        }

        // ----------- USER MANAGEMENT

        static void NewUser()
        {
            Console.Write("Username: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (Users.Any(u => u.Name == name))
            {
                Console.WriteLine("User already exists."); Wait(); return;
            }

            Console.Write("Starting balance (default 10000): ");
            string balInput = Console.ReadLine();
            decimal balance = decimal.TryParse(balInput, out var b) ? b : 10000m;

            Users.Add(new User { Name = name, Balance = balance });
            _repo.SaveAll(Users);
            Console.WriteLine($"✅ User '{name}' created with balance {balance:N2}");
            Wait();
        }

        static async Task SelectUser()
        {
            Console.Write("Username: ");
            string name = Console.ReadLine()?.Trim();
            var user = Users.FirstOrDefault(u => u.Name == name);
            if (user == null) { Console.WriteLine("❌ Not found."); Wait(); return; }
            await UserMenu(user);
        }

        static void AllUsersOverview()
        {
            Console.Clear();
            Header("ALL USERS");
            foreach (var u in Users)
            {
                string arrow = u.TotalPnL >= 0 ? "▲" : "▼";
                Console.WriteLine($"  👤 {u.Name,-20} | Positions: {u.Positions.Count,3} | " +
                                  $"Balance: {u.Balance,10:N2} | Exposure: {u.TotalExposure,10:N2} | " +
                                  $"P/L: {arrow} {Math.Abs(u.TotalPnL):N2}");
            }
            Wait();
        }

        static void SearchBySymbol()
        {
            Console.Write("Symbol: ");
            string sym = Console.ReadLine()?.Trim().ToUpper();
            Console.Clear();
            Header($"SEARCH: {sym}");
            bool found = false;

            foreach (var u in Users)
            {
                var p = u.Positions.FirstOrDefault(x => x.Symbol == sym);
                if (p == null) continue;
                found = true;
                string arrow = p.UnrealizedPnL >= 0 ? "▲" : "▼";
                Console.WriteLine($"  👤 {u.Name,-20} | Qty: {p.Quantity,6} | " +
                                  $"Avg: {p.AvgBuyPrice,8:N2} | Current: {p.CurrentPrice,8:N2} | " +
                                  $"P/L: {arrow} {Math.Abs(p.UnrealizedPnL):N2} ({p.UnrealizedPnLPercent:+0.00;-0.00}%)");
            }

            if (!found) Console.WriteLine("  No positions found.");
            Wait();
        }

        static void SetProvider()
        {
            Console.WriteLine("  1  →  Manual (no API)");
            Console.WriteLine("  2  →  Finnhub");
            Console.WriteLine("  3  →  AlphaVantage");
            Console.Write("Choice: ");
            string c = Console.ReadLine();

            if (c == "2")
            {
                Console.Write("Finnhub API Key: ");
                _market = new FinnhubProvider(Console.ReadLine());
                Console.WriteLine("✅ Finnhub active.");
            }
            else if (c == "3")
            {
                Console.Write("AlphaVantage API Key: ");
                _market = new AlphaVantageProvider(Console.ReadLine());
                Console.WriteLine("✅ AlphaVantage active.");
            }
            else
            {
                _market = new ManualPriceProvider();
                Console.WriteLine("✅ Manual mode.");
            }
            Wait();
        }

        //----USER MENU 

        static async Task UserMenu(User user)
        {
            while (true)
            {
                Console.Clear();
                Header($"{user.Name}  |  Balance: {user.Balance:N2}  |  P/L: {user.TotalPnL:+N2;-N2}");
                _risk.CheckStopLosses(user);
                Console.WriteLine("  1  →  Open Positions");
                Console.WriteLine("  2  →  Buy");
                Console.WriteLine("  3  →  Sell");
                Console.WriteLine("  4  →  Update Price (manual)");
                Console.WriteLine("  5  →  Refresh Prices (API)");
                Console.WriteLine("  6  →  Transfer Position");
                Console.WriteLine("  7  →  Portfolio Analysis");
                Console.WriteLine("  8  →  Performance Stats");
                Console.WriteLine("  9  →  Risk Calculator");
                Console.WriteLine("  10 →  Transaction History");
                Console.WriteLine("  0  →  Back");
                Divider();
                Console.Write("Choice: ");
                string c = Console.ReadLine();

                if      (c == "1")  ShowPositions(user);
                else if (c == "2")  Buy(user);
                else if (c == "3")  Sell(user);
                else if (c == "4")  UpdatePriceManual(user);
                else if (c == "5")  await RefreshPrices(user);
                else if (c == "6")  Transfer(user);
                else if (c == "7")  PortfolioAnalysis(user);
                else if (c == "8")  PerformanceStats(user);
                else if (c == "9")  RiskCalculator(user);
                else if (c == "10") ShowHistory(user);
                else if (c == "0")  break;
            }
        }

        // TRADING OPERATIONS

        static void Buy(User user)
        {
            Console.Write("Symbol: ");
            string sym = Console.ReadLine()?.Trim().ToUpper();
            Console.Write("Sector (e.g. Tech, Finance): ");
            string sector = Console.ReadLine()?.Trim() ?? "Unknown";
            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            { Console.WriteLine("Invalid quantity."); Wait(); return; }
            Console.Write("Buy Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price) || price <= 0)
            { Console.WriteLine("Invalid price."); Wait(); return; }
            Console.Write("Stop-Loss (0 to skip): ");
            decimal.TryParse(Console.ReadLine(), out decimal sl);

           
            var risk = _risk.CalculatePositionSize(user.Balance, price, sl > 0 ? sl : price * 0.95m);
            Console.WriteLine($"\n  Recommended size : {risk.PositionSize} shares ({risk.RiskAmount:N2} at risk)");
            Console.WriteLine($"  Portfolio exposure: {risk.ExposurePercent}%");
            if (!string.IsNullOrEmpty(risk.Warning)) Console.WriteLine($"  {risk.Warning}");

            Console.Write("\nConfirm? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y") return;

            _portfolio.Buy(user, sym, sector, qty, price, sl);
            Console.WriteLine($"✅ BUY {qty} {sym} @ {price:N2}");
            Wait();
        }

        static void Sell(User user)
        {
            Console.Write("Symbol: ");
            string sym = Console.ReadLine()?.Trim().ToUpper();
            var pos = user.Positions.FirstOrDefault(p => p.Symbol == sym);
            if (pos == null) { Console.WriteLine("Position not found."); Wait(); return; }

            Console.WriteLine($"  Holding {pos.Quantity} shares at avg {pos.AvgBuyPrice:N2}");
            Console.Write("Quantity to sell: ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            { Console.WriteLine("Invalid quantity."); Wait(); return; }
            Console.Write("Sell Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price) || price <= 0)
            { Console.WriteLine("Invalid price."); Wait(); return; }

            decimal realizedPnL = (price - pos.AvgBuyPrice) * qty;
            Console.WriteLine($"\n  Realized P/L on this trade: {realizedPnL:+N2;-N2}");
            Console.Write("Confirm? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y") return;

            _portfolio.Sell(user, sym, qty, price);
            Console.WriteLine($"✅ SELL {qty} {sym} @ {price:N2} | P/L: {realizedPnL:+N2;-N2}");
            Wait();
        }

        static void UpdatePriceManual(User user)
        {
            Console.Write("Symbol: ");
            string sym = Console.ReadLine()?.Trim().ToUpper();
            Console.Write("New Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            { Console.WriteLine("Invalid price."); Wait(); return; }
            _portfolio.UpdatePrice(user, sym, price);
            Console.WriteLine($"✅ {sym} updated to {price:N2}");
            Wait();
        }

        static async Task RefreshPrices(User user)
        {
            if (!user.Positions.Any()) { Console.WriteLine("No open positions."); Wait(); return; }

            Console.WriteLine($"  Fetching prices via {_market.ProviderName}...");
            foreach (var p in user.Positions)
            {
                decimal live = await _market.GetPriceAsync(p.Symbol);
                if (live > 0)
                {
                    p.CurrentPrice = live;
                    Console.WriteLine($"  {p.Symbol,-8} → {live:N2}");
                }
                else
                {
                    // API returned no dataa.
                    Console.Write($"  {p.Symbol} price not found. Enter manually: ");
                    if (decimal.TryParse(Console.ReadLine(), out decimal manual))
                        p.CurrentPrice = manual;
                }
            }
            _repo.SaveAll(Users);
            Console.WriteLine("✅ Prices updated.");
            Wait();
        }

        static void Transfer(User sender)
        {
            Console.Write("Recipient: ");
            string name = Console.ReadLine()?.Trim();
            var recipient = Users.FirstOrDefault(u => u.Name == name);
            if (recipient == null) { Console.WriteLine("User not found."); Wait(); return; }
            Console.Write("Symbol: ");
            string sym = Console.ReadLine()?.Trim().ToUpper();
            bool ok = _portfolio.Transfer(sender, recipient, sym);
            Console.WriteLine(ok ? $"✅ {sym} transferred to {name}." : "❌ Transfer failed.");
            Wait();
        }

        // ANALYTICS 
        static void ShowPositions(User user)
        {
            Console.Clear();
            Header($"{user.Name} — OPEN POSITIONS");
            _analytics.PrintHeatmap(user);
            _analytics.PrintPnLSummary(user);
            _risk.PrintRiskSummary(user);
            Wait();
        }

        static void PortfolioAnalysis(User user)
        {
            Console.Clear();
            Header($"{user.Name} — PORTFOLIO ANALYSIS");
            _analytics.PrintHeatmap(user);
            _analytics.PrintPnLSummary(user);
            _risk.PrintRiskSummary(user);
            Wait();
        }

        static void PerformanceStats(User user)
        {
            Console.Clear();
            Header($"{user.Name} — PERFORMANCE STATS");
            _analytics.PrintStats(user);
            Wait();
        }

        static void RiskCalculator(User user)
        {
            Console.Clear();
            Header("RISK CALCULATOR");
            Console.Write("Entry Price : ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal entry)) return;
            Console.Write("Stop-Loss   : ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal sl)) return;
            Console.Write("Risk % (default 1.0): ");
            string rInput = Console.ReadLine();
            double rPct = double.TryParse(rInput, out var r) ? r : 1.0;

            var result = _risk.CalculatePositionSize(user.Balance, entry, sl, rPct);
            Console.WriteLine();
            Console.WriteLine($"  Balance         : {user.Balance:N2}");
            Console.WriteLine($"  Risk Amount     : {result.RiskAmount:N2}");
            Console.WriteLine($"  Position Size   : {result.PositionSize} shares");
            Console.WriteLine($"  Exposure        : {result.ExposurePercent}% of balance");
            if (!string.IsNullOrEmpty(result.Warning))
                Console.WriteLine($"  {result.Warning}");
            Wait();
        }

        static void ShowHistory(User user)
        {
            Console.Clear();
            Header($"{user.Name} — TRANSACTION HISTORY");
            if (!user.History.Any()) { Console.WriteLine("No transactions yet."); Wait(); return; }

            foreach (var t in user.History.OrderByDescending(t => t.Date))
                Console.WriteLine($"  [{t.Date:yyyy-MM-dd HH:mm}]  {t.Type,-10}  {t.Symbol,-8}  " +
                                  $"Qty: {t.Quantity,6}  Price: {t.Price,10:N2}  |  {t.Note}");
            Wait();
        }

        // ─── UI HELPERS ───────────────────────────────────────────

        static void Header(string title)
        {
            Console.WriteLine("╔══════════════════════════════════════════════╗");
            Console.WriteLine($"║  {title,-44}║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");
        }

        static void Divider() => Console.WriteLine(new string('─', 48));

        static void Wait()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}

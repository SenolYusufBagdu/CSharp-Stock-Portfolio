using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// model for a single stock holding
class Stock
{
    public string Symbol { get; set; }
    public int Quantity { get; set; }
    public decimal BuyPrice { get; set; }      // original purchase price
    public decimal CurrentPrice { get; set; }  // latest market price
    public DateTime BuyDate { get; set; }

    // total value at purchase
    public decimal TotalBuyValue => Quantity * BuyPrice;

    // total value at current price
    public decimal TotalCurrentValue => Quantity * CurrentPrice;

    // profit or loss in currency
    public decimal ProfitLoss => TotalCurrentValue - TotalBuyValue;

    // profit or loss as percentage
    public decimal ProfitLossPercent => BuyPrice > 0
        ? Math.Round((CurrentPrice - BuyPrice) / BuyPrice * 100, 2)
        : 0;
}

// single transaction record for history tracking
class Transaction
{
    public string Type { get; set; }        // BUY / SELL / UPDATE / TRANSFER
    public string Symbol { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public string Note { get; set; }
}

// user model holding stocks and transaction history
class User
{
    public string Name { get; set; }
    public List<Stock> Stocks { get; set; } = new List<Stock>();
    public List<Transaction> History { get; set; } = new List<Transaction>();
}

class Program
{
    static List<User> users = new List<User>();
    static string dataFile = "portfolio.json";

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        LoadData();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════╗");
            Console.WriteLine("║     PORTFOLIO TRACKER        ║");
            Console.WriteLine("╚══════════════════════════════╝");
            Console.WriteLine("  1  →  Select User");
            Console.WriteLine("  2  →  New User");
            Console.WriteLine("  3  →  List All Users");
            Console.WriteLine("  4  →  Search by Symbol");
            Console.WriteLine("  0  →  Exit");
            Console.Write("\nChoice: ");
            string choice = Console.ReadLine();

            if (choice == "1") SelectUser();
            else if (choice == "2") NewUser();
            else if (choice == "3") ListAllUsers();
            else if (choice == "4") SearchBySymbol();
            else if (choice == "0") break;
            else Console.WriteLine("Invalid choice.");
        }
    }

    // ================= FILE OPERATIONS =================

    static void LoadData()
    {
        // if file doesn't exist, start with empty list
        if (!File.Exists(dataFile)) return;

        try
        {
            string json = File.ReadAllText(dataFile);
            users = JsonSerializer.Deserialize<List<User>>(json)
                    ?? new List<User>();
        }
        catch
        {
            // if file is corrupted, start fresh
            users = new List<User>();
        }
    }

    static void SaveData()
    {
        // save to json with readable formatting
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(dataFile, JsonSerializer.Serialize(users, options));
    }

    // ================= USER OPERATIONS =================

    static void NewUser()
    {
        Console.Write("Username: ");
        string name = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        // prevent duplicate usernames
        if (users.Any(u => u.Name == name))
        {
            Console.WriteLine("User already exists.");
            Wait(); return;
        }

        users.Add(new User { Name = name });
        SaveData();
        Console.WriteLine($"✅ User '{name}' created.");
        Wait();
    }

    static void SelectUser()
    {
        Console.Write("Username: ");
        string name = Console.ReadLine()?.Trim();
        var user = users.FirstOrDefault(u => u.Name == name);

        if (user == null) { Console.WriteLine("❌ User not found."); Wait(); return; }
        UserMenu(user);
    }

    static void ListAllUsers()
    {
        Console.Clear();
        Console.WriteLine("═══════════════ ALL USERS ═══════════════");

        if (!users.Any()) { Console.WriteLine("No users yet."); Wait(); return; }

        foreach (var u in users)
        {
            decimal invested = u.Stocks.Sum(s => s.TotalBuyValue);
            decimal current = u.Stocks.Sum(s => s.TotalCurrentValue);
            decimal pl = current - invested;
            string arrow = pl >= 0 ? "▲" : "▼";

            Console.WriteLine($"  👤 {u.Name,-20} | Stocks: {u.Stocks.Count,3} | " +
                              $"Invested: {invested,10:N2} | Value: {current,10:N2} | " +
                              $"P/L: {arrow} {Math.Abs(pl):N2}");
        }
        Wait();
    }

    static void SearchBySymbol()
    {
        Console.Write("Symbol: ");
        string symbol = Console.ReadLine()?.Trim().ToUpper();
        Console.Clear();
        Console.WriteLine($"═══════════════ RESULTS: {symbol} ═══════════════");

        bool found = false;
        foreach (var u in users)
        {
            var stock = u.Stocks.FirstOrDefault(s => s.Symbol == symbol);
            if (stock == null) continue;

            found = true;
            string arrow = stock.ProfitLoss >= 0 ? "▲" : "▼";
            Console.WriteLine($"  👤 {u.Name,-20} | Qty: {stock.Quantity,6} | " +
                              $"Buy: {stock.BuyPrice,8:N2} | Current: {stock.CurrentPrice,8:N2} | " +
                              $"P/L: {arrow} {Math.Abs(stock.ProfitLoss):N2} ({stock.ProfitLossPercent:+0.00;-0.00}%)");
        }

        if (!found) Console.WriteLine("No results found.");
        Wait();
    }

    // ================= USER MENU =================

    static void UserMenu(User u)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"╔══════════════════════════════╗");
            Console.WriteLine($"║  {u.Name,-28}║");
            Console.WriteLine($"╚══════════════════════════════╝");
            Console.WriteLine("  1  →  List Stocks");
            Console.WriteLine("  2  →  Add Stock");
            Console.WriteLine("  3  →  Remove Stock");
            Console.WriteLine("  4  →  Update Price");
            Console.WriteLine("  5  →  Transfer Stock");
            Console.WriteLine("  6  →  Portfolio Analysis");
            Console.WriteLine("  7  →  Transaction History");
            Console.WriteLine("  0  →  Back");
            Console.Write("\nChoice: ");
            string choice = Console.ReadLine();

            if (choice == "1") ListStocks(u);
            else if (choice == "2") AddStock(u);
            else if (choice == "3") RemoveStock(u);
            else if (choice == "4") UpdatePrice(u);
            else if (choice == "5") Transfer(u);
            else if (choice == "6") PortfolioAnalysis(u);
            else if (choice == "7") ShowHistory(u);
            else if (choice == "0") break;
        }
    }

    // ================= STOCK OPERATIONS =================

    static void ListStocks(User u)
    {
        Console.Clear();
        Console.WriteLine($"═══════════════ {u.Name}'s STOCKS ═══════════════");

        if (!u.Stocks.Any()) { Console.WriteLine("No stocks found."); Wait(); return; }

        Console.WriteLine($"  {"Symbol",-8} {"Qty",6} {"Buy Price",12} {"Current",12} {"P/L",12} {"P/L%",8}");
        Console.WriteLine(new string('─', 65));

        foreach (var s in u.Stocks)
        {
            string arrow = s.ProfitLoss >= 0 ? "▲" : "▼";
            Console.WriteLine($"  {s.Symbol,-8} {s.Quantity,6} {s.BuyPrice,12:N2} {s.CurrentPrice,12:N2} " +
                              $"{arrow}{Math.Abs(s.ProfitLoss),11:N2} {s.ProfitLossPercent,7:+0.00;-0.00}%");
        }

        Console.WriteLine(new string('─', 65));
        decimal totalInvested = u.Stocks.Sum(s => s.TotalBuyValue);
        decimal totalCurrent = u.Stocks.Sum(s => s.TotalCurrentValue);
        Console.WriteLine($"  {"TOTAL",-8} {"",6} {totalInvested,12:N2} {totalCurrent,12:N2} " +
                          $"{(totalCurrent - totalInvested > 0 ? "▲" : "▼")}{Math.Abs(totalCurrent - totalInvested),11:N2}");
        Wait();
    }

    static void AddStock(User u)
    {
        Console.Write("Symbol: ");
        string symbol = Console.ReadLine()?.Trim().ToUpper();
        Console.Write("Quantity: ");
        int qty = int.Parse(Console.ReadLine() ?? "0");
        Console.Write("Buy Price: ");
        decimal price = decimal.Parse(Console.ReadLine() ?? "0");

        // add stock to user portfolio
        u.Stocks.Add(new Stock
        {
            Symbol = symbol,
            Quantity = qty,
            BuyPrice = price,
            CurrentPrice = price,  // current starts at buy price
            BuyDate = DateTime.Now
        });

        // log this transaction to history
        LogTransaction(u, "BUY", symbol, qty, price, $"Purchased {qty} {symbol} at {price:N2}");
        SaveData();
        Console.WriteLine($"✅ {symbol} added.");
        Wait();
    }

    static void RemoveStock(User u)
    {
        Console.Write("Symbol to remove: ");
        string symbol = Console.ReadLine()?.Trim().ToUpper();
        var stock = u.Stocks.FirstOrDefault(s => s.Symbol == symbol);

        if (stock == null) { Console.WriteLine("Stock not found."); Wait(); return; }

        // log before removing
        LogTransaction(u, "SELL", symbol, stock.Quantity, stock.CurrentPrice,
                       $"Removed {symbol} from portfolio");
        u.Stocks.Remove(stock);
        SaveData();
        Console.WriteLine($"✅ {symbol} removed.");
        Wait();
    }

    static void UpdatePrice(User u)
    {
        // update current market price for a stock
        Console.Write("Symbol: ");
        string symbol = Console.ReadLine()?.Trim().ToUpper();
        var stock = u.Stocks.FirstOrDefault(s => s.Symbol == symbol);

        if (stock == null) { Console.WriteLine("Stock not found."); Wait(); return; }

        decimal oldPrice = stock.CurrentPrice;
        Console.Write($"New price (current: {oldPrice:N2}): ");
        decimal newPrice = decimal.Parse(Console.ReadLine() ?? "0");

        stock.CurrentPrice = newPrice;

        // log the price update
        LogTransaction(u, "UPDATE", symbol, stock.Quantity, newPrice,
                       $"Price updated: {oldPrice:N2} → {newPrice:N2}");
        SaveData();
        Console.WriteLine($"✅ {symbol} price updated to {newPrice:N2}");
        Wait();
    }

    static void Transfer(User sender)
    {
        Console.Write("Recipient username: ");
        string recipientName = Console.ReadLine()?.Trim();
        var recipient = users.FirstOrDefault(u => u.Name == recipientName);

        if (recipient == null) { Console.WriteLine("User not found."); Wait(); return; }

        Console.Write("Symbol to transfer: ");
        string symbol = Console.ReadLine()?.Trim().ToUpper();
        var stock = sender.Stocks.FirstOrDefault(s => s.Symbol == symbol);

        if (stock == null) { Console.WriteLine("Stock not found."); Wait(); return; }

        // move stock from sender to recipient
        recipient.Stocks.Add(stock);
        sender.Stocks.Remove(stock);

        // log for both users
        LogTransaction(sender, "TRANSFER", symbol, stock.Quantity, stock.CurrentPrice,
                       $"Transferred to {recipientName}");
        LogTransaction(recipient, "TRANSFER", symbol, stock.Quantity, stock.CurrentPrice,
                       $"Received from {sender.Name}");
        SaveData();
        Console.WriteLine($"✅ {symbol} transferred to {recipientName}.");
        Wait();
    }

    // ================= PORTFOLIO ANALYSIS =================

    static void PortfolioAnalysis(User u)
    {
        Console.Clear();
        Console.WriteLine($"═══════════════ PORTFOLIO ANALYSIS: {u.Name} ═══════════════");

        if (!u.Stocks.Any()) { Console.WriteLine("No stocks to analyze."); Wait(); return; }

        decimal totalInvested = u.Stocks.Sum(s => s.TotalBuyValue);
        decimal totalCurrent = u.Stocks.Sum(s => s.TotalCurrentValue);
        decimal totalPL = totalCurrent - totalInvested;
        decimal totalPLPct = totalInvested > 0
                                ? Math.Round(totalPL / totalInvested * 100, 2) : 0;

        // best performing stock
        var best = u.Stocks.OrderByDescending(s => s.ProfitLossPercent).First();

        // worst performing stock
        var worst = u.Stocks.OrderBy(s => s.ProfitLossPercent).First();

        // stocks in profit
        int profitable = u.Stocks.Count(s => s.ProfitLoss > 0);

        // stocks at a loss
        int losing = u.Stocks.Count(s => s.ProfitLoss < 0);

        Console.WriteLine();
        Console.WriteLine($"  Total Invested   : {totalInvested,14:N2}");
        Console.WriteLine($"  Portfolio Value  : {totalCurrent,14:N2}");
        Console.WriteLine($"  Total P/L        : {(totalPL >= 0 ? "▲" : "▼")} {Math.Abs(totalPL),12:N2}  ({totalPLPct:+0.00;-0.00}%)");
        Console.WriteLine();
        Console.WriteLine($"  Best Performer   : {best.Symbol,-8}  ▲ {best.ProfitLossPercent:+0.00}%  ({best.ProfitLoss:+N2})");
        Console.WriteLine($"  Worst Performer  : {worst.Symbol,-8}  {(worst.ProfitLossPercent >= 0 ? "▲" : "▼")} {worst.ProfitLossPercent:+0.00;-0.00}%  ({worst.ProfitLoss:+N2;-N2})");
        Console.WriteLine();
        Console.WriteLine($"  Profitable Stocks: {profitable}");
        Console.WriteLine($"  Losing Stocks    : {losing}");

        // largest position by current value
        var largest = u.Stocks.OrderByDescending(s => s.TotalCurrentValue).First();
        double weight = (double)(largest.TotalCurrentValue / totalCurrent * 100);
        Console.WriteLine($"  Largest Position : {largest.Symbol} ({weight:0.0}% of portfolio)");

        Wait();
    }

    // ================= TRANSACTION HISTORY =================

    static void ShowHistory(User u)
    {
        Console.Clear();
        Console.WriteLine($"═══════════════ TRANSACTION HISTORY: {u.Name} ═══════════════");

        if (!u.History.Any()) { Console.WriteLine("No transactions yet."); Wait(); return; }

        // show most recent transactions first
        foreach (var t in u.History.OrderByDescending(t => t.Date))
        {
            Console.WriteLine($"  [{t.Date:yyyy-MM-dd HH:mm}]  {t.Type,-10}  {t.Symbol,-8}  " +
                              $"Qty: {t.Quantity,6}  Price: {t.Price,10:N2}  |  {t.Note}");
        }
        Wait();
    }

    // log a transaction to user history
    static void LogTransaction(User u, string type, string symbol,
                                int qty, decimal price, string note)
    {
        u.History.Add(new Transaction
        {
            Type = type,
            Symbol = symbol,
            Quantity = qty,
            Price = price,
            Date = DateTime.Now,
            Note = note
        });
    }

    // ================= HELPERS =================

    static void Wait()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}

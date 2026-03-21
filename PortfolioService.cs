// PortfolioService.cs — core buy/sell/transfer logic, position averaging, realized p/l

using System;
using System.Linq;
using PortfolioEngine.Models;
using PortfolioEngine.Repository;

namespace PortfolioEngine.Services
{
    public class PortfolioService
    {
        private readonly UserRepository _repo;

        public PortfolioService(UserRepository repo)
        {
            _repo = repo;
        }

        // buy a stock — merges into existing position if symbol already held
        public void Buy(User user, string symbol, string sector, int qty, decimal price, decimal stopLoss = 0)
        {
            var existing = user.Positions.FirstOrDefault(p => p.Symbol == symbol);

            if (existing != null)
            {
                // position averaging — recalculate weighted average price
                int newQty = existing.Quantity + qty;
                decimal newAvg = ((existing.Quantity * existing.AvgBuyPrice) + (qty * price)) / newQty;

                existing.Quantity = newQty;
                existing.AvgBuyPrice = Math.Round(newAvg, 4);
                existing.CurrentPrice = price;

                // update stop-loss if provided
                if (stopLoss > 0) existing.StopLoss = stopLoss;

                Log(user, "BUY", symbol, qty, price,
                    $"Added to position. New avg: {newAvg:N4}, total qty: {newQty}");
            }
            else
            {
                // open new position
                user.Positions.Add(new Position
                {
                    Symbol       = symbol,
                    Sector       = sector,
                    Quantity     = qty,
                    AvgBuyPrice  = price,
                    CurrentPrice = price,
                    StopLoss     = stopLoss,
                    OpenDate     = DateTime.Now
                });

                Log(user, "BUY", symbol, qty, price, $"New position opened at {price:N2}");
            }

            _repo.SaveAll(GetAllUsers(user));
        }

        // partial or full sell — calculates realized p/l on the closed portion
        public bool Sell(User user, string symbol, int qty, decimal price)
        {
            var position = user.Positions.FirstOrDefault(p => p.Symbol == symbol);

            if (position == null)
            {
                Console.WriteLine("Position not found.");
                return false;
            }

            if (qty > position.Quantity)
            {
                Console.WriteLine($"Cannot sell {qty} — only {position.Quantity} held.");
                return false;
            }

            // calculate realized p/l on the sold portion
            decimal realizedPnL = (price - position.AvgBuyPrice) * qty;
            user.RealizedPnL += realizedPnL;

            // record the closed trade for performance analytics
            user.ClosedTrades.Add(new ClosedTrade
            {
                Symbol    = symbol,
                Quantity  = qty,
                BuyPrice  = position.AvgBuyPrice,
                SellPrice = price,
                OpenDate  = position.OpenDate,
                CloseDate = DateTime.Now
            });

            if (qty == position.Quantity)
            {
                // full close — remove position entirely
                user.Positions.Remove(position);
                Log(user, "SELL", symbol, qty, price,
                    $"Position closed. Realized P/L: {realizedPnL:+N2;-N2}");
            }
            else
            {
                // partial close — reduce quantity, keep position open
                position.Quantity -= qty;
                position.CurrentPrice = price;
                Log(user, "SELL", symbol, qty, price,
                    $"Partial sell. Remaining: {position.Quantity}. Realized P/L: {realizedPnL:+N2;-N2}");
            }

            _repo.SaveAll(GetAllUsers(user));
            return true;
        }

        // transfer a position to another user
        public bool Transfer(User sender, User recipient, string symbol)
        {
            var position = sender.Positions.FirstOrDefault(p => p.Symbol == symbol);
            if (position == null) return false;

            recipient.Positions.Add(position);
            sender.Positions.Remove(position);

            Log(sender,    "TRANSFER", symbol, position.Quantity, position.CurrentPrice,
                $"Transferred to {recipient.Name}");
            Log(recipient, "TRANSFER", symbol, position.Quantity, position.CurrentPrice,
                $"Received from {sender.Name}");

            _repo.SaveAll(GetAllUsers(sender));
            return true;
        }

        // update current market price for a position
        public void UpdatePrice(User user, string symbol, decimal newPrice)
        {
            var position = user.Positions.FirstOrDefault(p => p.Symbol == symbol);
            if (position == null) return;

            decimal old = position.CurrentPrice;
            position.CurrentPrice = newPrice;

            Log(user, "UPDATE", symbol, position.Quantity, newPrice,
                $"Price: {old:N2} → {newPrice:N2}");

            _repo.SaveAll(GetAllUsers(user));
        }

        // log a transaction to user history
        private void Log(User user, string type, string symbol, int qty, decimal price, string note)
        {
            user.History.Add(new Transaction
            {
                Type     = type,
                Symbol   = symbol,
                Quantity = qty,
                Price    = price,
                Date     = DateTime.Now,
                Note     = note
            });
        }

        // helper — gets all users for save (caller passes one user as context)
        // in a real app this would come from repository directly
        private System.Collections.Generic.List<User> GetAllUsers(User context)
        {
            // this is resolved by Program.cs passing the full list
            // kept simple for single-file architecture
            return Program.Users;
        }
    }
}

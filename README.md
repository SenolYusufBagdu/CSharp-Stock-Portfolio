# CSharp Stock Portfolio — Quantitative Portfolio Engine
### Professional-grade portfolio management and trading analytics system in C#

![Language](https://img.shields.io/badge/language-C%23-blue)
![Platform](https://img.shields.io/badge/platform-.NET%208-purple)
![Architecture](https://img.shields.io/badge/architecture-modular-green)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

---

## Overview

Started as a simple stock tracker. Evolved into a modular quantitative portfolio engine with professional risk management, realized/unrealized P/L tracking, performance analytics and live market data integration.

Built to practice real-world C# architecture patterns used in fintech systems.

---

## Architecture

```
PortfolioEngine/
├── Models.cs                 — Position, ClosedTrade, User, RiskResult, PerformanceStats
├── Repository.cs             — JSON persistence layer
├── IMarketDataProvider.cs    — Interface + Finnhub + AlphaVantage implementations
├── PortfolioService.cs       — Buy, Sell, Transfer, UpdatePrice
├── RiskService.cs            — Position sizing, stop-loss, exposure tracking
├── AnalyticsService.cs       — Win rate, expectancy, profit factor, heatmap
└── Program.cs                — Console UI, wires all services together
```

---

## Features

### Core Trading Operations
- **Position Averaging** — buying the same symbol merges into existing position with weighted average price
- **Partial Sell** — sell any portion of a position, keeps remainder open
- **Stock Transfer** — move positions between users
- **Multi-user support** — separate portfolios per user

### Realized vs Unrealized P/L
- Realized P/L tracked on every closed or partial trade
- Unrealized P/L calculated on all open positions
- Total P/L = Realized + Unrealized shown at all times

### Risk Management Engine
- Position sizing formula: `positionSize = (balance × riskPercent) / (entry - stopLoss)`
- Stop-loss tracking per position with live breach alerts
- Portfolio exposure tracking (% of balance deployed)
- Concentration warnings for oversized positions

### Performance Analytics (Quant Level)
- Win Rate
- Average Win / Average Loss
- Risk/Reward Ratio
- Expectancy: `(WinRate × AvgWin) - (LossRate × AvgLoss)`
- Profit Factor: `Gross Profit / Gross Loss`
- Trade count, realized P/L summary

### Portfolio Heatmap
- All positions ranked by portfolio weight
- Sector breakdown and concentration
- Color-coded P/L per position
- Largest position highlight

### Live Market Data (Interface-based)
- **Manual** — default, no API needed
- **Finnhub** — plug in API key, auto-fetch prices
- **AlphaVantage** — alternative provider
- Providers are swappable via `IMarketDataProvider` interface

### Transaction History
- Every BUY / SELL / UPDATE / TRANSFER logged
- Timestamped with notes
- Sorted by most recent

---

## Tech Stack

| Component | Detail |
|-----------|--------|
| Language | C# |
| Platform | .NET 8 |
| Storage | JSON via System.Text.Json |
| Data Access | LINQ |
| HTTP Client | System.Net.Http |
| Architecture | Service layer + Repository pattern |

---

## Getting Started

```bash
git clone https://github.com/SenolYusufBagdu/CSharp-Stock-Portfolio.git
cd CSharp-Stock-Portfolio
dotnet run
```

---

## Usage

```
╔══════════════════════════════════════════════╗
║  QUANTITATIVE PORTFOLIO ENGINE               ║
╚══════════════════════════════════════════════╝
  1  →  Select User
  2  →  New User
  3  →  All Users Overview
  4  →  Search by Symbol
  5  →  Set Market Data Provider
  0  →  Exit
```

User menu:

```
  1  →  Open Positions
  2  →  Buy
  3  →  Sell (partial or full)
  4  →  Update Price (manual)
  5  →  Refresh Prices (API)
  6  →  Transfer Position
  7  →  Portfolio Analysis
  8  →  Performance Stats
  9  →  Risk Calculator
  10 →  Transaction History
```

---

## Risk Calculator Example

```
Entry Price  : 150.00
Stop-Loss    : 142.50
Risk %       : 1.0%
Balance      : 10,000.00

→ Risk Amount    : 100.00
→ Position Size  : 13 shares
→ Exposure       : 19.5% of balance
```

---

## Roadmap

- [ ] Yahoo Finance / Polygon.io integration
- [ ] CSV / Excel export
- [ ] Price alerts
- [ ] Backtesting module
- [ ] ASP.NET Web UI
- [ ] Strategy tagging per trade

---

## License

MIT License — free to use and modify.

---

## Author

Built by **Şenol Yusuf Bağdu**
Game Developer & Programmer | Algorithmic Trading | Learning Python & Quantitative Finance

[LinkedIn](https://linkedin.com/in/senol-yusuf-bagdu) · [GitHub](https://github.com/SenolYusufBagdu)

# ASP.NET Tx Board

ASP.NET Tx Board is a transaction monitoring and diagnostics package for ASP.NET Core applications. It captures HTTP request timing, database transaction behavior, SQL execution metadata, and provides a built-in dashboard/API for analysis.

Inspired by `spring-tx-board`:
https://github.com/Mamun-Al-Babu-Shikder/spring-tx-board

## Requirements

- .NET 10 SDK/runtime
- ASP.NET Core application
- Entity Framework Core (recommended, for transaction and SQL tracking)

## Install

```bash
dotnet add package AspNet.Tx.Board
```

## Quick Start

In `Program.cs`:

```csharp
using AspNet.Tx.Board.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTxBoard(builder.Configuration);

var app = builder.Build();

app.UseTxBoard();
app.Run();
```

## EF Core Interceptors (Important)

For database transaction and SQL visibility, add Tx Board interceptors to your `DbContext` options:

```csharp
using AspNet.Tx.Board.Interceptors;

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite("Data Source=app.db");

    options.AddInterceptors(
        sp.GetRequiredService<TxBoardTransactionInterceptor>(),
        sp.GetRequiredService<TxBoardCommandInterceptor>(),
        sp.GetRequiredService<TxBoardConnectionInterceptor>()
    );
});
```

## Configuration

`appsettings.json`:

```json
{
  "TxBoard": {
    "Enabled": true,
    "LogType": "Simple",
    "Storage": "InMemory",
    "AlarmingThreshold": {
      "Transaction": 1000,
      "Connection": 1000
    },
    "DurationBuckets": [100, 500, 1000, 2000, 5000],
    "Redis": {
      "EntityTtl": "7.00:00:00"
    }
  }
}
```

### Options

| Key | Description | Default |
| --- | --- | --- |
| `Enabled` | Enables/disables Tx Board capture | `true` |
| `LogType` | `Simple` or `Details` logging format | `Simple` |
| `Storage` | `InMemory` or `Redis` | `InMemory` |
| `AlarmingThreshold.Transaction` | Warn threshold for transaction duration (ms) | `1000` |
| `AlarmingThreshold.Connection` | Warn threshold for connection occupied time (ms) | `1000` |
| `DurationBuckets` | Duration histogram buckets (ms) | `[100,500,1000,2000,5000]` |
| `Redis.EntityTtl` | Retention TTL for Redis mode | `7 days` |

Note: `Storage: Redis` currently falls back to in-memory storage with a warning log.

## Endpoints

### Dashboard and HTTP Metrics

- `GET /tx-board/ui`
- `GET /tx-board/api/transactions`
- `GET /tx-board/api/distribution`
- `GET /tx-board/api/export`

### Spring TX Board-Compatible APIs

- `GET /api/tx-board/config/alarming-threshold`
- `GET /api/tx-board/tx-summary`
- `GET /api/tx-board/tx-charts`
- `GET /api/tx-board/tx-logs`
- `GET /api/tx-board/sql-logs`

## Logging

- `Simple` mode:
  - Info for healthy transactions
  - Warning for alarming transactions or connection usage
- `Details` mode:
  - Structured multi-line transaction/sql diagnostics

## Maintainer

Built and maintained by Tamzid.

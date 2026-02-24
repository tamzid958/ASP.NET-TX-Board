# ASP.NET Tx Board

ASP.NET Tx Board is a transaction monitoring and diagnostics package for ASP.NET Core applications. It captures HTTP request timing, database transaction behavior, SQL execution metadata, and provides a built-in dashboard/API for analysis.

Inspired by `spring-tx-board`:
https://github.com/Mamun-Al-Babu-Shikder/spring-tx-board

## Requirements

- .NET 10 SDK/runtime
- ASP.NET Core application
- Entity Framework Core (recommended, for transaction and SQL tracking)

<br>
<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/f99ea307-8927-4f01-9049-26bf7043cd51" />


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

### ASP.NET TX Board-Compatible APIs

- `GET /api/tx-board/config/alarming-threshold`
- `GET /api/tx-board/tx-summary`
- `GET /api/tx-board/tx-charts`
- `GET /api/tx-board/tx-logs`
- `GET /api/tx-board/sql-logs`

## OpenTelemetry

Tx Board emits traces and metrics via the standard .NET `System.Diagnostics` APIs — **no extra NuGet packages** are required in the library itself.

### Traces

A span named `db.transaction` is started for every database transaction and carries the following tags:

| Tag | Example |
| --- | --- |
| `db.transaction.method` | `OrderService.PlaceOrder` |
| `db.transaction.isolation_level` | `ReadCommitted` |
| `db.transaction.propagation` | `Required` / `Nested` |
| `db.transaction.status` | `Committed` / `RolledBack` / `Errored` |
| `db.transaction.query_count` | `4` |
| `db.transaction.alarming` | `false` |

### Metrics

| Metric | Unit | Tags |
| --- | --- | --- |
| `tx_board.transaction.duration` | ms | `db.transaction.method`, `db.transaction.status`, `db.transaction.propagation` |
| `tx_board.connection.duration` | ms | `db.connection.alarming` |
| `tx_board.request.duration` | ms | `http.method`, `http.route`, `http.status_code` |

### Setup

Install your preferred OTel exporter (e.g. `OpenTelemetry.Exporter.Console`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`) and register the Tx Board source/meter:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("AspNet.Tx.Board")
        .AddConsoleExporter())
    .WithMetrics(m => m
        .AddMeter("AspNet.Tx.Board")
        .AddConsoleExporter());
```

When no OTel listener is registered, all instrumentation is a no-op with zero overhead.

## Logging

- `Simple` mode:
  - Info for healthy transactions
  - Warning for alarming transactions or connection usage
- `Details` mode:
  - Structured multi-line transaction/sql diagnostics

## Maintainer

Built and maintained by Tamzid.

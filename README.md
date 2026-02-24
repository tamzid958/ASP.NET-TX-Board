# ASP.NET Tx Board – Installation & Usage Instructions

**ASP.NET Tx Board** is an intelligent, auto-configurable transaction monitoring and diagnostics package for ASP.NET Core applications. It provides deep visibility into transactional behavior — capturing execution time, nested transactions, executed SQL queries, connection usage, and post-transaction activity.

This document explains how to install, configure, and use the package in an ASP.NET Core project.

---

## 1. Prerequisites

* .NET 6.0 or later
* ASP.NET Core Web API / MVC application
* Entity Framework Core (recommended for database transaction tracking)

---

## 2. Install the Package

Install the NuGet package:

### Using .NET CLI

```bash
dotnet add package AspNet.Tx.Board
```

### Using Package Manager Console

```powershell
Install-Package AspNet.Tx.Board
```

---

## 3. Register the Service

In `Program.cs` (for .NET 6+ minimal hosting model):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Tx Board
builder.Services.AddTxBoard(builder.Configuration);

var app = builder.Build();

// Enable middleware
app.UseTxBoard();

app.Run();
```

---

## 4. Configuration

Add configuration to `appsettings.json`:

```json
{
  "TxBoard": {
    "Enabled": true,
    "LogType": "Simple", // Simple | Details
    "Storage": "InMemory", // InMemory | Redis
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

### Configuration Options

| Property                        | Description                                     |
| ------------------------------- | ----------------------------------------------- |
| `Enabled`                       | Enables or disables Tx Board                    |
| `LogType`                       | Logging format: `Simple` or `Details`           |
| `Storage`                       | Storage type: `InMemory` or `Redis`             |
| `AlarmingThreshold.Transaction` | Highlight transactions exceeding duration (ms)  |
| `AlarmingThreshold.Connection`  | Highlight connections exceeding lease time (ms) |
| `DurationBuckets`               | Buckets for duration distribution               |
| `Redis.EntityTtl`               | Log retention time in Redis                     |

---

## 5. Web UI

If your application includes MVC or minimal APIs, the built-in dashboard is available at:

```
http://localhost:5000/tx-board/ui
```

The dashboard provides:

* Real-time transaction monitoring
* Filtering and sorting
* Pagination
* Duration distribution
* CSV export

---

## 6. Logging Modes

Tx Board emits logs when transactions complete.

### Simple Mode (default)

Healthy transaction (INFO):

```
Transaction [OrderService.PlaceOrder] took 152 ms, Status: Committed
```

Unhealthy transaction (WARN):

```
Transaction [OrderService.PlaceOrder] took 2150 ms, Status: Committed, Connections: 3, Queries: 12
```

---

### Details Mode

Healthy (INFO):

```
[Tx-Board] Transaction Completed:
  • ID: 4bfd0935-2de3-4992-96da-1992431d48c1
  • Method: OrderService.PlaceOrder
  • Isolation Level: ReadCommitted
  • Status: Committed
  • Started At: 2026-02-24T10:15:30Z
  • Ended At: 2026-02-24T10:15:30Z
  • Duration: 152 ms
  • Connections Acquired: 2
  • Executed Query Count: 5
```

Unhealthy transactions log at `Warning` level.

---

## 7. Using Transactions

Tx Board automatically hooks into:

* `TransactionScope`
* EF Core `DbContext` transactions
* `IDbContextTransaction`
* Middleware-based request pipeline tracking

### Example Using EF Core

```csharp
public class OrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task PlaceOrderAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        // Business logic
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
```

No additional attributes or manual instrumentation required.

---

## 8. Storage Options

### InMemory (Default)

* Thread-safe in-memory collection
* Suitable for single-instance applications

### Redis

* Distributed log storage
* Suitable for multi-instance deployments
* Supports configurable TTL

---

## 9. Transaction-less SQL Logging

Simple:

```
SQL executor leased connection for 2009 ms to execute 3 queries
```

Details:

```
[Tx-Board] SQL Execution Completed:
  • ID: e127a497-f92d-4ef3-b686-23b7b0503aa7
  • Connection Occupied Time: 2015 ms
  • Executed Query Count: 1
  • Executed Queries:
    └── SELECT * FROM Employees WHERE Age >= 30;
```

---

## 10. Duration Distribution Utility

Transactions are grouped into configurable ranges such as:

* `0–100ms`
* `100–500ms`
* `500–1000ms`
* `1000ms+`

Used for performance analysis and dashboard visualization.

---

## 11. Exporting Transactions

Transactions can be exported in CSV format from:

```
/tx-board/api/export
```

Supports filtering and date-range selection.

---

## 12. Future Enhancements

* Kafka streaming support
* ELK stack integration
* Azure Application Insights integration
* OpenTelemetry support

---

## 13. Maintainer

**ASP.NET Tx Board**
Built and maintained by Tamzid
Inspired by:
- SDLC.PRO
- https://github.com/Mamun-Al-Babu-Shikder/spring-tx-board

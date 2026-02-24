# AspNet.Tx.Board

AspNet.Tx.Board is a transaction monitoring and diagnostics package for ASP.NET Core.

## Inspiration

Inspired by `spring-tx-board`:
https://github.com/Mamun-Al-Babu-Shikder/spring-tx-board

## Install

```bash
dotnet add package AspNet.Tx.Board
```

## Usage

Register services and middleware in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTxBoard(builder.Configuration);

var app = builder.Build();
app.UseTxBoard();
app.Run();
```

## Configuration

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

## Endpoints

- `/tx-board/ui`
- `/tx-board/api/transactions`
- `/tx-board/api/distribution`
- `/tx-board/api/export`

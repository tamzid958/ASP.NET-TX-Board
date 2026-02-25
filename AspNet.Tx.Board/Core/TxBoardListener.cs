using System.Diagnostics;
using System.Text;
using AspNet.Tx.Board.Enums;
using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Storage;
using AspNet.Tx.Board.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Core;

public sealed class TxBoardListener
{
    // Per-async-context transaction context.
    // AsyncLocal flows the REFERENCE to child contexts — mutations to the shared
    // TransactionContext object are visible across all continuations in the same chain.
    private static readonly AsyncLocal<TransactionContext?> _ctxLocal = new();

    private readonly ITransactionLogRepository _txRepo;
    private readonly ISqlExecutionLogRepository _sqlRepo;
    private readonly TxBoardOptions _options;
    private readonly ILogger<TxBoardListener> _logger;

    public TxBoardListener(
        ITransactionLogRepository txRepo,
        ISqlExecutionLogRepository sqlRepo,
        IOptions<TxBoardOptions> options,
        ILogger<TxBoardListener> logger)
    {
        _txRepo = txRepo;
        _sqlRepo = sqlRepo;
        _options = options.Value;
        _logger = logger;
    }

    // ── Request context initialization ────────────────────────────────────────

    /// <summary>
    /// Called by TxBoardMiddleware at the start of each HTTP request.
    /// Creates a fresh TransactionContext reference that flows to all child async
    /// continuations (EF Core interceptors) within this request.
    /// </summary>
    public void InitializeRequestContext()
    {
        if (_options.Enabled)
            _ctxLocal.Value = new TransactionContext();
    }

    // ── Transaction lifecycle ──────────────────────────────────────────────────

    public void OnTransactionBegin(System.Data.IsolationLevel isolation, string? method)
    {
        if (!_options.Enabled) return;

        // Use the pre-initialized context (set by middleware) or create one for
        // non-HTTP flows (e.g., EnsureCreated at startup, background jobs).
        var ctx = GetOrCreateContext();
        var stack = ctx.TxStack;

        bool isRoot = stack.Count == 0;
        TransactionInfo? parent = isRoot ? null : stack.Peek();

        var info = new TransactionInfo
        {
            TxId = isRoot ? Guid.NewGuid() : (Guid?)null,
            Method = method ?? ResolveCallerMethod(),
            Isolation = MapIsolation(isolation),
            Propagation = isRoot ? TxPropagation.Required : TxPropagation.Nested,
            StartTime = DateTimeOffset.UtcNow,
            Thread = GetThreadName(),
            IsRoot = isRoot
        };

        if (isRoot)
        {
            info.Events =
            [
                new() { Type = TransactionEventType.TransactionStart, Timestamp = info.StartTime, Details = $"Begin {info.Method}" }
            ];
        }

        parent?.Children.Add(info);
        stack.Push(info);

        // Start an OTel span for this transaction.
        // ActivitySource.StartActivity returns null when no listener is subscribed — no overhead.
        if (_options.EnableTelemetry)
        {
            info.Activity = TxBoardTelemetry.ActivitySource.StartActivity(
                "db.transaction", ActivityKind.Internal);
            info.Activity?.SetTag("db.transaction.method", info.Method);
            info.Activity?.SetTag("db.transaction.isolation_level", info.Isolation.ToString());
            info.Activity?.SetTag("db.transaction.propagation", info.Propagation.ToString());
        }
    }

    public void OnAfterCommit() => CompleteTransaction(TransactionStatus.Committed);

    public void OnAfterRollback() => CompleteTransaction(TransactionStatus.RolledBack);

    public void OnTransactionError() => CompleteTransaction(TransactionStatus.Errored);

    // ── Connection lifecycle ───────────────────────────────────────────────────

    public void OnConnectionAcquired()
    {
        if (!_options.Enabled) return;

        var ctx = _ctxLocal.Value;
        var stack = ctx?.TxStack;
        if (stack?.Count > 0)
        {
            var tx = stack.Peek();
            tx.AcquiredConnectionCount++;
            tx.LastConnectionAcquiredAt = DateTimeOffset.UtcNow;

            GetRoot(stack)?.Events?.Add(new TransactionEvent
            {
                Type = TransactionEventType.ConnectionAcquired,
                Timestamp = DateTimeOffset.UtcNow,
                Details = $"Connection #{tx.AcquiredConnectionCount} acquired"
            });
        }
        else
        {
            var c = ctx ?? (_ctxLocal.Value = new TransactionContext());
            if (c.SqlInfo == null)
            {
                c.SqlInfo = new SqlExecutionInfo
                {
                    ConAcquiredTime = DateTimeOffset.UtcNow,
                    Thread = GetThreadName()
                };
            }
        }
    }

    public void OnConnectionClosed()
    {
        if (!_options.Enabled) return;

        var ctx = _ctxLocal.Value;
        var stack = ctx?.TxStack;
        if (stack?.Count > 0)
        {
            var tx = stack.Peek();
            if (tx.LastConnectionAcquiredAt.HasValue)
            {
                var occupiedMs = (long)(DateTimeOffset.UtcNow - tx.LastConnectionAcquiredAt.Value).TotalMilliseconds;
                tx.TotalConnectionOccupiedMs += occupiedMs;
                if (occupiedMs > _options.AlarmingThreshold.Connection)
                    tx.AlarmingConnectionCount++;
                tx.LastConnectionAcquiredAt = null;
            }

            GetRoot(stack)?.Events?.Add(new TransactionEvent
            {
                Type = TransactionEventType.ConnectionReleased,
                Timestamp = DateTimeOffset.UtcNow,
                Details = "Connection released"
            });
        }
        else if (ctx?.SqlInfo != null)
        {
            var sqlInfo = ctx.SqlInfo;
            sqlInfo.ConReleaseTime = DateTimeOffset.UtcNow;
            var occupiedMs = (long)(sqlInfo.ConReleaseTime.Value - sqlInfo.ConAcquiredTime).TotalMilliseconds;

            bool alarming = occupiedMs > _options.AlarmingThreshold.Connection;

            var log = new SqlExecutionLog
            {
                Id = sqlInfo.Id,
                ConAcquiredTime = sqlInfo.ConAcquiredTime,
                ConReleaseTime = sqlInfo.ConReleaseTime.Value,
                ConOccupiedTime = occupiedMs,
                AlarmingConnection = alarming,
                Thread = sqlInfo.Thread,
                ExecutedQueries = [.. sqlInfo.Queries]
            };

            if (_options.EnableTelemetry)
            {
                TxBoardTelemetry.ConnectionDuration.Record(
                    occupiedMs,
                    new TagList { { "db.connection.alarming", alarming } });
            }

            _sqlRepo.Save(log);
            LogSqlExecution(log);
            ctx.SqlInfo = null;
        }
    }

    // ── SQL tracking ──────────────────────────────────────────────────────────

    public void OnSqlExecuted(string? sql)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(sql)) return;

        var ctx = _ctxLocal.Value;
        var stack = ctx?.TxStack;
        if (stack?.Count > 0)
        {
            var tx = stack.Peek();
            if (tx.IsCompleted)
                tx.PostTransactionQueries.Add(sql);
            else
                tx.Queries.Add(sql);
        }
        else
        {
            ctx?.SqlInfo?.Queries.Add(sql);
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private TransactionContext GetOrCreateContext()
    {
        if (_ctxLocal.Value is { } existing)
            return existing;

        var ctx = new TransactionContext();
        _ctxLocal.Value = ctx;
        return ctx;
    }

    private void CompleteTransaction(TransactionStatus status)
    {
        if (!_options.Enabled) return;

        var ctx = _ctxLocal.Value;
        var stack = ctx?.TxStack;
        if (stack == null || stack.Count == 0) return;

        var info = stack.Pop();
        info.EndTime = DateTimeOffset.UtcNow;
        info.Status = status;
        info.IsCompleted = true;

        // Record end event on root
        var root = stack.Count > 0 ? GetRoot(stack) : info;
        root?.Events?.Add(new TransactionEvent
        {
            Type = TransactionEventType.TransactionEnd,
            Timestamp = info.EndTime.Value,
            Details = $"End {info.Method} ({status})"
        });

        var durationMs = info.EndTime.HasValue
            ? (long)(info.EndTime.Value - info.StartTime).TotalMilliseconds
            : 0L;

        // Finalise OTel span
        if (_options.EnableTelemetry && info.Activity is { } activity)
        {
            activity.SetTag("db.transaction.status", status.ToString());
            activity.SetTag("db.transaction.query_count", info.Queries.Count);
            if (info.IsRoot)
                activity.SetTag("db.transaction.alarming", durationMs > _options.AlarmingThreshold.Transaction);

            activity.SetStatus(status == TransactionStatus.Errored
                ? ActivityStatusCode.Error
                : ActivityStatusCode.Ok);
            activity.Stop();
        }

        // Record OTel metric (only for root transactions to avoid double-counting)
        if (info.IsRoot)
        {
            if (_options.EnableTelemetry)
            {
                TxBoardTelemetry.TransactionDuration.Record(
                    durationMs,
                    new TagList
                    {
                        { "db.transaction.method", info.Method },
                        { "db.transaction.status", status.ToString() },
                        { "db.transaction.propagation", info.Propagation.ToString() }
                    });
            }

            var log = BuildTransactionLog(info);
            _txRepo.Save(log);
            LogTransaction(log);
            if (ctx != null) ctx.SqlInfo = null;
        }
    }

    private TransactionLog BuildTransactionLog(TransactionInfo info)
    {
        var endTime = info.EndTime ?? DateTimeOffset.UtcNow;
        var duration = (long)(endTime - info.StartTime).TotalMilliseconds;

        ConnectionSummary? connSummary = null;
        bool? connectionOriented = false;
        bool? havingAlarmingConnection = null;

        if (info.AcquiredConnectionCount > 0)
        {
            connectionOriented = true;
            connSummary = new ConnectionSummary
            {
                AcquisitionCount = info.AcquiredConnectionCount,
                AlarmingConnectionCount = info.AlarmingConnectionCount,
                OccupiedTime = info.TotalConnectionOccupiedMs
            };
            havingAlarmingConnection = info.AlarmingConnectionCount > 0;
        }

        return new TransactionLog
        {
            TxId = info.TxId,
            Method = info.Method,
            Propagation = info.Propagation,
            Isolation = info.Isolation,
            StartTime = info.StartTime,
            EndTime = endTime,
            Duration = duration,
            Status = info.Status,
            Thread = info.Thread,
            ExecutedQueries = [.. info.Queries],
            PostTransactionQueries = [.. info.PostTransactionQueries],
            Child = info.Children.Select(BuildTransactionLog).ToList(),
            Events = info.Events ?? [],
            ConnectionSummary = connSummary,
            ConnectionOriented = connectionOriented,
            AlarmingTransaction = duration > _options.AlarmingThreshold.Transaction,
            HavingAlarmingConnection = havingAlarmingConnection
        };
    }

    private static TransactionInfo? GetRoot(Stack<TransactionInfo> stack) =>
        stack.Count > 0 ? stack.Last() : null;

    private static string GetThreadName()
    {
        var t = System.Threading.Thread.CurrentThread;
        return t.Name ?? $"Thread-{t.ManagedThreadId}";
    }

    private static TxIsolationLevel MapIsolation(System.Data.IsolationLevel level) => level switch
    {
        System.Data.IsolationLevel.ReadUncommitted => TxIsolationLevel.ReadUncommitted,
        System.Data.IsolationLevel.ReadCommitted => TxIsolationLevel.ReadCommitted,
        System.Data.IsolationLevel.RepeatableRead => TxIsolationLevel.RepeatableRead,
        System.Data.IsolationLevel.Serializable => TxIsolationLevel.Serializable,
        _ => TxIsolationLevel.Default
    };

    private static readonly string[] SkipNamespacePrefixes =
    [
        "AspNet.Tx.Board",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions",
        "Microsoft.AspNetCore",
        "System",
        "Npgsql",
        "MySql",
        "Oracle",
        "SQLitePCL",
    ];

    private static string ResolveCallerMethod()
    {
        try
        {
            var frames = new StackTrace(skipFrames: 3, fNeedFileInfo: false).GetFrames();
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null) continue;
                var ns = method.DeclaringType?.Namespace ?? string.Empty;
                if (SkipNamespacePrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal)))
                    continue;
                if (method.DeclaringType?.Name.StartsWith('<') == true)
                    continue; // skip compiler-generated state machine types
                return $"{method.DeclaringType?.Name}.{method.Name}";
            }
        }
        catch { /* best-effort */ }
        return "Unknown";
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void LogTransaction(TransactionLog log)
    {
        bool isAlarming = log.AlarmingTransaction || log.HavingAlarmingConnection == true;

        if (_options.LogType == TxBoardLogType.Details)
        {
            var msg = BuildDetailsMessage(log);
            if (isAlarming)
                _logger.LogWarning("{Message}", msg);
            else
                _logger.LogInformation("{Message}", msg);
            return;
        }

        // SIMPLE mode
        if (isAlarming)
        {
            _logger.LogWarning(
                "Transaction [{Method}] took {Duration} ms, Status: {Status}, Connections: {Connections}, Queries: {Queries}",
                log.Method, log.Duration, log.Status,
                log.ConnectionSummary?.AcquisitionCount ?? 0,
                log.ExecutedQueries.Count);
        }
        else
        {
            _logger.LogInformation(
                "Transaction [{Method}] took {Duration} ms, Status: {Status}",
                log.Method, log.Duration, log.Status);
        }
    }

    private static string BuildDetailsMessage(TransactionLog log)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Tx-Board] Transaction Completed:");
        sb.AppendLine($"  • ID: {log.TxId}");
        sb.AppendLine($"  • Method: {log.Method}");
        sb.AppendLine($"  • Propagation: {log.Propagation}");
        sb.AppendLine($"  • Isolation: {log.Isolation}");
        sb.AppendLine($"  • Status: {log.Status}");
        sb.AppendLine($"  • Started At: {log.StartTime:O}");
        sb.AppendLine($"  • Ended At: {log.EndTime:O}");
        sb.AppendLine($"  • Duration: {log.Duration} ms");
        sb.AppendLine($"  • Connections Acquired: {log.ConnectionSummary?.AcquisitionCount ?? 0}");
        sb.AppendLine($"  • Executed Query Count: {log.ExecutedQueries.Count}");
        sb.AppendLine($"  • Post Transaction Query Count: {log.PostTransactionQueries.Count}");

        if (log.Child.Count > 0)
        {
            sb.AppendLine("  • Inner Transactions:");
            for (int i = 0; i < log.Child.Count; i++)
            {
                var child = log.Child[i];
                var prefix = i == log.Child.Count - 1 ? "    └──" : "    ├──";
                sb.AppendLine($"{prefix} {child.Method} (Duration: {child.Duration} ms, Propagation: {child.Propagation}, Isolation: {child.Isolation}, Status: {child.Status})");
            }
        }

        return sb.ToString();
    }

    private void LogSqlExecution(SqlExecutionLog log)
    {
        if (_options.LogType == TxBoardLogType.Details)
        {
            var msg = $"[Tx-Board] SQL Execution Completed:\n" +
                      $"  • ID: {log.Id}\n" +
                      $"  • Connection Acquired At: {log.ConAcquiredTime:O}\n" +
                      $"  • Connection Released At: {log.ConReleaseTime:O}\n" +
                      $"  • Connection Occupied Time: {log.ConOccupiedTime} ms\n" +
                      $"  • Executed Query Count: {log.ExecutedQueries.Count}";

            if (log.AlarmingConnection)
                _logger.LogWarning("{Message}", msg);
            else
                _logger.LogInformation("{Message}", msg);
        }
        else if (log.AlarmingConnection)
        {
            _logger.LogWarning(
                "SQL executor leased connection for {Occupied} ms to execute {Count} queries",
                log.ConOccupiedTime, log.ExecutedQueries.Count);
        }
    }
}

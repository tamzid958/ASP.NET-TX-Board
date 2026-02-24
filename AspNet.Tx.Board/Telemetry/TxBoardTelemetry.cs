using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AspNet.Tx.Board.Telemetry;

/// <summary>
/// OpenTelemetry instrumentation sources and metric instruments for AspNet.Tx.Board.
/// Both <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// are built into .NET — no extra NuGet packages are required by this library.
/// </summary>
/// <remarks>
/// <para>
/// To capture distributed traces, register the activity source in your <c>TracerProvider</c>:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(TxBoardTelemetry.SourceName));
/// </code>
/// </para>
/// <para>
/// To capture metrics, register the meter in your <c>MeterProvider</c>:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(m => m.AddMeter(TxBoardTelemetry.SourceName));
/// </code>
/// </para>
/// </remarks>
public static class TxBoardTelemetry
{
    /// <summary>
    /// The activity source and meter name for AspNet.Tx.Board.
    /// Pass this value to <c>AddSource()</c> / <c>AddMeter()</c> when configuring OpenTelemetry.
    /// </summary>
    public const string SourceName = "AspNet.Tx.Board";

    // ── Traces ────────────────────────────────────────────────────────────────

    /// <summary>
    /// ActivitySource used to create spans for database transactions and connections.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(SourceName);

    // ── Metrics ───────────────────────────────────────────────────────────────

    internal static readonly Meter Meter = new(SourceName);

    /// <summary>
    /// Histogram of database transaction durations in milliseconds.
    /// Tags: <c>db.transaction.method</c>, <c>db.transaction.status</c>, <c>db.transaction.propagation</c>.
    /// </summary>
    internal static readonly Histogram<long> TransactionDuration =
        Meter.CreateHistogram<long>(
            "tx_board.transaction.duration",
            unit: "ms",
            description: "Duration of a database transaction in milliseconds.");

    /// <summary>
    /// Histogram of the time (ms) a database connection was held open for non-transactional SQL.
    /// Tags: <c>db.connection.alarming</c>.
    /// </summary>
    internal static readonly Histogram<long> ConnectionDuration =
        Meter.CreateHistogram<long>(
            "tx_board.connection.duration",
            unit: "ms",
            description: "Time a database connection was occupied for non-transactional SQL execution.");

    /// <summary>
    /// Histogram of HTTP request durations in milliseconds as observed by the TxBoard middleware.
    /// Tags: <c>http.method</c>, <c>http.route</c>, <c>http.status_code</c>.
    /// </summary>
    internal static readonly Histogram<long> RequestDuration =
        Meter.CreateHistogram<long>(
            "tx_board.request.duration",
            unit: "ms",
            description: "Duration of HTTP requests tracked by the TxBoard middleware.");
}

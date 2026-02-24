using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNet.Tx.Board.Domain;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static IApplicationBuilder MapTxBoardEndpoints(this IApplicationBuilder app)
    {
        if (app is not WebApplication webApp)
            return app;

        // ── Existing HTTP-request monitoring API ──────────────────────────────

        webApp.MapGet("/tx-board/api/transactions", (
            ITxBoardStore store,
            DateTimeOffset? from, DateTimeOffset? to,
            bool? unhealthyOnly, int? skip, int? take) =>
            Results.Ok(store.Query(from, to, unhealthyOnly, skip ?? 0, take ?? 100)));

        webApp.MapGet("/tx-board/api/distribution", (
            ITxBoardStore store, IOptions<TxBoardOptions> opts,
            DateTimeOffset? from, DateTimeOffset? to) =>
            Results.Ok(store.GetDurationDistribution(opts.Value.DurationBuckets, from, to)));

        webApp.MapGet("/tx-board/api/export", (
            ITxBoardStore store, DateTimeOffset? from, DateTimeOffset? to, bool? unhealthyOnly) =>
        {
            var records = store.Query(from, to, unhealthyOnly, 0, 1000);
            return Results.File(Encoding.UTF8.GetBytes(BuildCsv(records)), "text/csv", "tx-board-export.csv");
        });

        // ── Spring TX Board-compatible DB transaction API ─────────────────────

        webApp.MapGet("/api/tx-board/config/alarming-threshold", (IOptions<TxBoardOptions> opts) =>
            Results.Json(new
            {
                transaction = opts.Value.AlarmingThreshold.Transaction,
                connection = opts.Value.AlarmingThreshold.Connection
            }, JsonOptions));

        webApp.MapGet("/api/tx-board/tx-summary", (ITransactionLogRepository repo) =>
            Results.Json(repo.GetSummary(), JsonOptions));

        webApp.MapGet("/api/tx-board/tx-charts", (ITransactionLogRepository repo) =>
            Results.Json(new { durationDistribution = repo.GetDurationDistributions() }, JsonOptions));

        webApp.MapGet("/api/tx-board/tx-logs", (
            ITransactionLogRepository repo,
            int? page, int? size, string? sort,
            string? search, string? status, string? propagation,
            string? isolation, bool? connectionOriented) =>
        {
            var (sortField, sortDir) = ParseSort(sort, "startTime");
            var request = new PageRequest
            {
                Page = page ?? 0,
                Size = Math.Clamp(size ?? 10, 1, 1000),
                SortField = sortField,
                SortDirection = sortDir,
                Search = search,
                Status = status,
                Propagation = propagation,
                Isolation = isolation,
                ConnectionOriented = connectionOriented
            };
            return Results.Json(repo.FindAll(request), JsonOptions);
        });

        webApp.MapGet("/api/tx-board/sql-logs", (
            ISqlExecutionLogRepository repo,
            int? page, int? size, string? sort, string? search) =>
        {
            var (sortField, sortDir) = ParseSort(sort, "conAcquiredTime");
            var request = new PageRequest
            {
                Page = page ?? 0,
                Size = Math.Clamp(size ?? 10, 1, 1000),
                SortField = sortField,
                SortDirection = sortDir,
                Search = search
            };
            return Results.Json(repo.FindAll(request), JsonOptions);
        });

        // ── UI static files served from embedded resources ────────────────────

        webApp.MapGet("/tx-board/ui", () => ServeEmbeddedResource("index.html", "text/html"));
        webApp.MapGet("/tx-board/ui/script.js", () => ServeEmbeddedResource("script.js", "application/javascript"));
        webApp.MapGet("/tx-board/ui/styles.css", () => ServeEmbeddedResource("styles.css", "text/css"));

        return app;
    }

    private static IResult ServeEmbeddedResource(string fileName, string contentType)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"AspNet.Tx.Board.Resources.{fileName}";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return Results.NotFound();
        using var reader = new StreamReader(stream);
        return Results.Content(reader.ReadToEnd(), contentType);
    }

    private static (string field, string direction) ParseSort(string? sort, string defaultField)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return (defaultField, "desc");

        var parts = sort.Split(',', 2);
        return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "asc");
    }

    private static string BuildCsv(IReadOnlyList<Models.TxRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Method,Status,StartedAt,EndedAt,DurationMs,IsUnhealthy,Path,HttpMethod");
        foreach (var r in records)
        {
            sb.AppendLine(string.Join(",",
                Escape(r.Id.ToString()), Escape(r.Method), Escape(r.Status),
                Escape(r.StartedAt.ToString("O")), Escape(r.EndedAt.ToString("O")),
                Escape(r.DurationMs.ToString()), Escape(r.IsUnhealthy.ToString()),
                Escape(r.Path), Escape(r.HttpMethod)));
        }
        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}

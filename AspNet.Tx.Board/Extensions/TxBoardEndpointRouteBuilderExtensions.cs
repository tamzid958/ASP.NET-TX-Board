using System.Text;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Storage;
using AspNet.Tx.Board.Ui;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardEndpointRouteBuilderExtensions
{
    public static IApplicationBuilder MapTxBoardEndpoints(this IApplicationBuilder app)
    {
        if (app is not WebApplication webApplication)
        {
            return app;
        }

        webApplication.MapGet("/tx-board/api/transactions", (
            ITxBoardStore store,
            DateTimeOffset? from,
            DateTimeOffset? to,
            bool? unhealthyOnly,
            int? skip,
            int? take) =>
        {
            var records = store.Query(from, to, unhealthyOnly, skip ?? 0, take ?? 100);
            return Results.Ok(records);
        });

        webApplication.MapGet("/tx-board/api/distribution", (ITxBoardStore store, IOptions<TxBoardOptions> options, DateTimeOffset? from, DateTimeOffset? to) =>
        {
            var distribution = store.GetDurationDistribution(options.Value.DurationBuckets, from, to);
            return Results.Ok(distribution);
        });

        webApplication.MapGet("/tx-board/api/export", (ITxBoardStore store, DateTimeOffset? from, DateTimeOffset? to, bool? unhealthyOnly) =>
        {
            var records = store.Query(from, to, unhealthyOnly, skip: 0, take: 1000);
            var csv = BuildCsv(records);
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", "tx-board-export.csv");
        });

        webApplication.MapGet("/tx-board/ui", () => Results.Content(TxBoardUiPage.Html, "text/html"));

        return app;
    }

    private static string BuildCsv(IReadOnlyList<Models.TxRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Method,Status,StartedAt,EndedAt,DurationMs,IsUnhealthy,Path,HttpMethod");

        foreach (var record in records)
        {
            sb.AppendLine(string.Join(",",
                Escape(record.Id.ToString()),
                Escape(record.Method),
                Escape(record.Status),
                Escape(record.StartedAt.ToString("O")),
                Escape(record.EndedAt.ToString("O")),
                Escape(record.DurationMs.ToString()),
                Escape(record.IsUnhealthy.ToString()),
                Escape(record.Path),
                Escape(record.HttpMethod)));
        }

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}

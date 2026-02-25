using System.Diagnostics;
using AspNet.Tx.Board.Core;
using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Services;
using AspNet.Tx.Board.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Middleware;

public sealed class TxBoardMiddleware
{
    private readonly RequestDelegate _next;

    public TxBoardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITxBoardRecorder recorder, IOptionsMonitor<TxBoardOptions> options, TxBoardListener listener)
    {
        var currentOptions = options.CurrentValue;

        if (!currentOptions.Enabled)
        {
            await _next(context);
            return;
        }

        // Initialize a fresh TransactionContext for this request. Because TransactionContext
        // is a reference type, AsyncLocal flows the same reference to all child async
        // continuations (EF Core interceptors), so mutations are visible everywhere.
        listener.InitializeRequestContext();

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var durationMs = stopwatch.ElapsedMilliseconds;
            var status = ResolveStatus(context.Response.StatusCode, exception);

            if (currentOptions.EnableTelemetry)
            {
                TxBoardTelemetry.RequestDuration.Record(
                    durationMs,
                    new TagList
                    {
                        { "http.method", context.Request.Method },
                        { "http.route", context.Request.Path.Value },
                        { "http.status_code", context.Response.StatusCode }
                    });
            }

            var record = new TxRecord
            {
                Method = string.IsNullOrWhiteSpace(context.GetEndpoint()?.DisplayName)
                    ? $"{context.Request.Method} {context.Request.Path}"
                    : context.GetEndpoint()!.DisplayName!,
                HttpMethod = context.Request.Method,
                Path = context.Request.Path.Value,
                StartedAt = startedAt,
                EndedAt = DateTimeOffset.UtcNow,
                DurationMs = durationMs,
                Status = status,
                ConnectionCount = 0,
                ExecutedQueryCount = 0
            };

            await recorder.RecordAsync(record, context.RequestAborted);
        }
    }

    private static string ResolveStatus(int statusCode, Exception? exception)
    {
        if (exception is not null)
        {
            return "Failed";
        }

        return statusCode >= 500 ? "Failed" : "Committed";
    }
}

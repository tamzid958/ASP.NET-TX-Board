using System.Diagnostics;
using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Services;
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

    public async Task InvokeAsync(HttpContext context, ITxBoardRecorder recorder, IOptionsMonitor<TxBoardOptions> options)
    {
        var currentOptions = options.CurrentValue;

        if (!currentOptions.Enabled)
        {
            await _next(context);
            return;
        }

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

            var record = new TxRecord
            {
                Method = string.IsNullOrWhiteSpace(context.GetEndpoint()?.DisplayName)
                    ? $"{context.Request.Method} {context.Request.Path}"
                    : context.GetEndpoint()!.DisplayName!,
                HttpMethod = context.Request.Method,
                Path = context.Request.Path.Value,
                StartedAt = startedAt,
                EndedAt = DateTimeOffset.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = ResolveStatus(context.Response.StatusCode, exception),
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

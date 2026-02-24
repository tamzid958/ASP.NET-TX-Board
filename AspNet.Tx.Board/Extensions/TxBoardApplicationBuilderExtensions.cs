using Microsoft.AspNetCore.Builder;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardApplicationBuilderExtensions
{
    /// <summary>
    /// Registers AspNet.Tx.Board middleware and maps all dashboard endpoints.
    /// Call this after <c>app.UseRouting()</c>.
    /// </summary>
    public static IApplicationBuilder UseTxBoard(this IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.TxBoardMiddleware>();
        app.MapTxBoardEndpoints();
        return app;
    }
}

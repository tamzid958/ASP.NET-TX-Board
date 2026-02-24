using Microsoft.AspNetCore.Builder;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTxBoard(this IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.TxBoardMiddleware>();
        app.MapTxBoardEndpoints();

        return app;
    }
}

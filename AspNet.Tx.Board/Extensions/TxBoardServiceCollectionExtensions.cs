using AspNet.Tx.Board.Middleware;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Services;
using AspNet.Tx.Board.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardServiceCollectionExtensions
{
    public static IServiceCollection AddTxBoard(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<TxBoardOptions>()
            .Bind(configuration.GetSection("TxBoard"))
            .ValidateDataAnnotations();

        services.AddSingleton<ITxBoardStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TxBoardOptions>>().Value;

            return options.Storage == TxBoardStorageType.Redis
                ? new RedisTxBoardStoreFallback(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisTxBoardStoreFallback>>())
                : new InMemoryTxBoardStore();
        });

        services.AddSingleton<ITxBoardRecorder, TxBoardRecorder>();

        return services;
    }
}

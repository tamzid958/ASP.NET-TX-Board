using AspNet.Tx.Board.Core;
using AspNet.Tx.Board.Interceptors;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Services;
using AspNet.Tx.Board.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Extensions;

public static class TxBoardServiceCollectionExtensions
{
    /// <summary>
    /// Registers AspNet.Tx.Board services. Configure via <c>appsettings.json</c> under the "TxBoard" key.
    /// </summary>
    public static IServiceCollection AddTxBoard(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<TxBoardOptions>()
            .Bind(configuration.GetSection("TxBoard"))
            .ValidateDataAnnotations();

        // HTTP-request level store (original feature — monitors HTTP requests)
        services.AddSingleton<ITxBoardStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TxBoardOptions>>().Value;
            return options.Storage == TxBoardStorageType.Redis
                ? new RedisTxBoardStoreFallback(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisTxBoardStoreFallback>>())
                : new InMemoryTxBoardStore();
        });

        services.AddSingleton<ITxBoardRecorder, TxBoardRecorder>();

        // Database transaction tracking repositories
        services.AddSingleton<ITransactionLogRepository, InMemoryTransactionLogRepository>();
        services.AddSingleton<ISqlExecutionLogRepository, InMemorySqlExecutionLogRepository>();

        // Core listener — singleton that uses AsyncLocal for per-request state
        services.AddSingleton<TxBoardListener>();

        // EF Core interceptors (register as singletons for injection into DbContextOptionsBuilder)
        services.AddSingleton<TxBoardCommandInterceptor>();
        services.AddSingleton<TxBoardTransactionInterceptor>();
        services.AddSingleton<TxBoardConnectionInterceptor>();

        return services;
    }
}

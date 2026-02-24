using AspNet.Tx.Board.Domain;
using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Storage;

public interface ITransactionLogRepository
{
    void Save(TransactionLog log);
    PageResponse<TransactionLog> FindAll(PageRequest request);
    TransactionSummary GetSummary();
    List<DurationDistribution> GetDurationDistributions();
}

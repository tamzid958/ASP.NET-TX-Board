using AspNet.Tx.Board.Domain;
using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Storage;

public interface ISqlExecutionLogRepository
{
    void Save(SqlExecutionLog log);
    PageResponse<SqlExecutionLog> FindAll(PageRequest request);
}

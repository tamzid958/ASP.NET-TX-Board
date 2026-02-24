using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Services;

public interface ITxBoardRecorder
{
    Task RecordAsync(TxRecord record, CancellationToken cancellationToken = default);
}

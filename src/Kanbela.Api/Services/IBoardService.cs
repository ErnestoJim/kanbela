using Kanbela.Api.Contracts;

namespace Kanbela.Api.Services;

public interface IBoardService
{
    Task<IReadOnlyList<BoardSummaryDto>> GetBoardsAsync(CancellationToken cancellationToken = default);

    Task<BoardDto?> GetBoardAsync(Guid boardId, CancellationToken cancellationToken = default);

    Task<CardCreatedResult> CreateCardAsync(
        Guid columnId,
        string title,
        string? description,
        CancellationToken cancellationToken = default);

    Task<BoardDto> MoveCardAsync(
        Guid boardId,
        Guid cardId,
        Guid targetColumnId,
        int newPosition,
        CancellationToken cancellationToken = default);
}

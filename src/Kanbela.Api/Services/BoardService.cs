using Microsoft.EntityFrameworkCore;
using Kanbela.Api.Contracts;
using Kanbela.Api.Data;
using Kanbela.Api.Domain;

namespace Kanbela.Api.Services;

public sealed class BoardService(KanbelaDbContext db) : IBoardService
{
    public async Task<IReadOnlyList<BoardSummaryDto>> GetBoardsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Boards
            .AsNoTracking()
            .OrderBy(board => board.Title)
            .Select(board => new BoardSummaryDto(
                board.Id,
                board.Title,
                board.Columns.Count,
                board.Columns.SelectMany(column => column.Cards).Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var board = await db.Boards
            .AsNoTracking()
            .Include(board => board.Columns)
            .ThenInclude(column => column.Cards)
            .FirstOrDefaultAsync(board => board.Id == boardId, cancellationToken);

        return board is null ? null : MapBoard(board);
    }

    public async Task<CardCreatedResult> CreateCardAsync(
        Guid columnId,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var column = await db.Columns
            .FirstOrDefaultAsync(candidate => candidate.Id == columnId, cancellationToken)
            ?? throw new BoardNotFoundException($"No se encontró la columna '{columnId}'.");

        var lastPosition = await db.Cards
            .Where(card => card.ColumnId == columnId)
            .MaxAsync(card => (int?)card.Position, cancellationToken);

        var card = new Card
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ColumnId = columnId,
            Position = lastPosition is null ? 0 : lastPosition.Value + 1,
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken);

        var board = await GetBoardAsync(column.BoardId, cancellationToken)
            ?? throw new BoardNotFoundException($"No se encontró el tablero '{column.BoardId}'.");

        return new CardCreatedResult(MapCard(card), board);
    }

    public async Task<BoardDto> MoveCardAsync(
        Guid boardId,
        Guid cardId,
        Guid targetColumnId,
        int newPosition,
        CancellationToken cancellationToken = default)
    {
        // Load the card together with its current column so we can validate ownership.
        var card = await db.Cards
            .Include(candidate => candidate.Column)
            .FirstOrDefaultAsync(candidate => candidate.Id == cardId, cancellationToken);

        if (card?.Column is null)
        {
            throw new BoardNotFoundException($"No se encontró la tarjeta '{cardId}'.");
        }

        if (card.Column.BoardId != boardId)
        {
            throw new BoardNotFoundException($"La tarjeta '{cardId}' no pertenece al tablero '{boardId}'.");
        }

        var targetColumn = await db.Columns
            .FirstOrDefaultAsync(column => column.Id == targetColumnId, cancellationToken);

        if (targetColumn is null || targetColumn.BoardId != boardId)
        {
            throw new BoardNotFoundException(
                $"La columna destino '{targetColumnId}' no existe en el tablero '{boardId}'.");
        }

        var sourceColumnId = card.ColumnId;

        // Remove the card from the equation, then insert it at the requested index.
        // This is the same operation for same-column reorders and cross-column moves.
        var targetCards = await db.Cards
            .Where(candidate => candidate.ColumnId == targetColumnId && candidate.Id != cardId)
            .OrderBy(candidate => candidate.Position)
            .ToListAsync(cancellationToken);

        var insertionIndex = Math.Clamp(newPosition, 0, targetCards.Count);
        targetCards.Insert(insertionIndex, card);

        card.ColumnId = targetColumnId;

        for (var index = 0; index < targetCards.Count; index++)
        {
            targetCards[index].Position = index;
        }

        // Close the gap left behind when the card changes column.
        if (sourceColumnId != targetColumnId)
        {
            var sourceCards = await db.Cards
                .Where(candidate => candidate.ColumnId == sourceColumnId && candidate.Id != cardId)
                .OrderBy(candidate => candidate.Position)
                .ToListAsync(cancellationToken);

            for (var index = 0; index < sourceCards.Count; index++)
            {
                sourceCards[index].Position = index;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetBoardAsync(boardId, cancellationToken)
            ?? throw new BoardNotFoundException($"No se encontró el tablero '{boardId}'.");
    }

    private static BoardDto MapBoard(Board board)
    {
        var columns = board.Columns
            .OrderBy(column => column.Order)
            .Select(column => new ColumnDto(
                column.Id,
                column.Name,
                column.Order,
                column.Cards
                    .OrderBy(card => card.Position)
                    .Select(MapCard)
                    .ToList()))
            .ToList();

        return new BoardDto(board.Id, board.Title, columns);
    }

    private static CardDto MapCard(Card card) =>
        new(card.Id, card.Title, card.Description, card.Position, card.ColumnId);
}

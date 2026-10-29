using Microsoft.AspNetCore.SignalR;
using Kanbela.Api.Services;

namespace Kanbela.Api.Hubs;

public sealed class BoardHub(IBoardService boardService, ILogger<BoardHub> logger) : Hub
{
    public const string BoardUpdatedEvent = "BoardUpdated";

    public static string GroupName(Guid boardId) => $"board-{boardId}";

    public async Task JoinBoard(string boardId)
    {
        if (!Guid.TryParse(boardId, out var parsedBoardId))
        {
            throw new HubException("El identificador del tablero no es válido.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(parsedBoardId));
        logger.LogInformation(
            "Conexión {ConnectionId} unida al grupo {Group}",
            Context.ConnectionId,
            GroupName(parsedBoardId));
    }

    public async Task LeaveBoard(string boardId)
    {
        if (!Guid.TryParse(boardId, out var parsedBoardId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(parsedBoardId));
    }

    /// <summary>
    /// Persists the move and notifies every other client in the board group.
    /// The caller already applied the move optimistically on its own UI.
    /// </summary>
    public async Task MoveCard(string boardId, Guid cardId, Guid targetColumnId, int newPosition)
    {
        if (!Guid.TryParse(boardId, out var parsedBoardId))
        {
            throw new HubException("El identificador del tablero no es válido.");
        }

        try
        {
            var board = await boardService.MoveCardAsync(
                parsedBoardId,
                cardId,
                targetColumnId,
                newPosition,
                Context.ConnectionAborted);

            await Clients
                .OthersInGroup(GroupName(parsedBoardId))
                .SendAsync(BoardUpdatedEvent, board, Context.ConnectionAborted);
        }
        catch (BoardNotFoundException exception)
        {
            logger.LogWarning(exception, "No se pudo mover la tarjeta {CardId}", cardId);
            throw new HubException(exception.Message);
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using Kanbela.Api.Contracts;
using Kanbela.Api.Hubs;
using Kanbela.Api.Services;

namespace Kanbela.Api.Endpoints;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/boards", async (IBoardService boardService, CancellationToken cancellationToken) =>
        {
            var boards = await boardService.GetBoardsAsync(cancellationToken);
            return Results.Ok(boards);
        });

        api.MapGet("/boards/{id:guid}", async (
            Guid id,
            IBoardService boardService,
            CancellationToken cancellationToken) =>
        {
            var board = await boardService.GetBoardAsync(id, cancellationToken);
            return board is null
                ? Results.Problem($"No se encontró el tablero '{id}'.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(board);
        });

        api.MapPost("/cards", async (
            CreateCardRequest request,
            IBoardService boardService,
            IHubContext<BoardHub> hub,
            CancellationToken cancellationToken) =>
        {
            var title = request.Title?.Trim();
            var errors = new Dictionary<string, string[]>();

            if (request.ColumnId == Guid.Empty)
            {
                errors["columnId"] = ["La columna es obligatoria."];
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                errors["title"] = ["El título es obligatorio."];
            }
            else if (title.Length > 200)
            {
                errors["title"] = ["El título no puede superar los 200 caracteres."];
            }

            if (request.Description is { Length: > 2000 })
            {
                errors["description"] = ["La descripción no puede superar los 2000 caracteres."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            try
            {
                var result = await boardService.CreateCardAsync(
                    request.ColumnId,
                    title!,
                    request.Description,
                    cancellationToken);

                await hub.Clients
                    .Group(BoardHub.GroupName(result.Board.Id))
                    .SendAsync(BoardHub.BoardUpdatedEvent, result.Board, cancellationToken);

                return Results.Created($"/api/boards/{result.Board.Id}", result.Card);
            }
            catch (BoardNotFoundException exception)
            {
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        api.MapPut("/cards/move", async (
            MoveCardRequest request,
            IBoardService boardService,
            IHubContext<BoardHub> hub,
            CancellationToken cancellationToken) =>
        {
            if (request.BoardId == Guid.Empty ||
                request.CardId == Guid.Empty ||
                request.TargetColumnId == Guid.Empty)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["move"] = ["boardId, cardId y targetColumnId son obligatorios."],
                });
            }

            try
            {
                var board = await boardService.MoveCardAsync(
                    request.BoardId,
                    request.CardId,
                    request.TargetColumnId,
                    request.NewPosition,
                    cancellationToken);

                // REST fallback clients also get the real-time board snapshot.
                await hub.Clients
                    .Group(BoardHub.GroupName(request.BoardId))
                    .SendAsync(BoardHub.BoardUpdatedEvent, board, cancellationToken);

                return Results.Ok(board);
            }
            catch (BoardNotFoundException exception)
            {
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        return app;
    }
}

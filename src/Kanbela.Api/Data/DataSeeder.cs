using Microsoft.EntityFrameworkCore;
using Kanbela.Api.Domain;

namespace Kanbela.Api.Data;

/// <summary>
/// Creates a demo board on first run so the client has something to show.
/// </summary>
public static class DataSeeder
{
    public static readonly Guid DemoBoardId = Guid.Parse("8a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    public static async Task SeedAsync(KanbelaDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Boards.AnyAsync(cancellationToken))
        {
            return;
        }

        var todo = new BoardColumn
        {
            Id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Name = "Por hacer",
            Order = 0,
            BoardId = DemoBoardId,
        };

        var doing = new BoardColumn
        {
            Id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            Name = "En progreso",
            Order = 1,
            BoardId = DemoBoardId,
        };

        var done = new BoardColumn
        {
            Id = Guid.Parse("33333333-3333-4333-8333-333333333333"),
            Name = "Hecho",
            Order = 2,
            BoardId = DemoBoardId,
        };

        todo.Cards.Add(new Card
        {
            Id = Guid.Parse("aaaaaaa1-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Title = "Definir el alcance del MVP",
            Description = "Acordar las funcionalidades mínimas del tablero colaborativo.",
            Position = 0,
            ColumnId = todo.Id,
        });

        doing.Cards.Add(new Card
        {
            Id = Guid.Parse("bbbbbbb2-bbbb-4bbb-8bbb-bbbbbbbbbbb2"),
            Title = "Implementar el tablero Kanban",
            Description = "Columnas, tarjetas y arrastrar y soltar en tiempo real con SignalR.",
            Position = 0,
            ColumnId = doing.Id,
        });

        db.Boards.Add(new Board
        {
            Id = DemoBoardId,
            Title = "Mi Proyecto",
            Columns = [todo, doing, done],
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

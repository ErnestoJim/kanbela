using Microsoft.EntityFrameworkCore;
using Kanbela.Api.Data;
using Kanbela.Api.Domain;
using Kanbela.Api.Services;

namespace Kanbela.Tests;

public sealed class BoardServiceTests
{
    [Fact]
    public async Task MoveCard_BetweenColumns_InsertsAtRequestedPositionAndRenumbersBothColumns()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var board = await fixture.Service.MoveCardAsync(
            fixture.BoardId,
            fixture.TodoAId,
            fixture.DoingId,
            newPosition: 1);

        var todo = board.Columns.Single(column => column.Id == fixture.TodoId);
        var doing = board.Columns.Single(column => column.Id == fixture.DoingId);

        Assert.Equal(new[] { fixture.TodoBId, fixture.TodoCId }, todo.Cards.Select(card => card.Id));
        Assert.Equal(new[] { 0, 1 }, todo.Cards.Select(card => card.Position));

        Assert.Equal(new[] { fixture.DoingAId, fixture.TodoAId, fixture.DoingBId }, doing.Cards.Select(card => card.Id));
        Assert.Equal(new[] { 0, 1, 2 }, doing.Cards.Select(card => card.Position));
        Assert.All(doing.Cards, card => Assert.Equal(fixture.DoingId, card.ColumnId));

        var persistedCard = await fixture.Db.Cards
            .AsNoTracking()
            .SingleAsync(card => card.Id == fixture.TodoAId);

        Assert.Equal(fixture.DoingId, persistedCard.ColumnId);
        Assert.Equal(1, persistedCard.Position);
    }

    [Fact]
    public async Task MoveCard_WithinSameColumn_ReordersAndKeepsPositionsContiguous()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var board = await fixture.Service.MoveCardAsync(
            fixture.BoardId,
            fixture.TodoAId,
            fixture.TodoId,
            newPosition: 2);

        var todo = board.Columns.Single(column => column.Id == fixture.TodoId);

        Assert.Equal(new[] { fixture.TodoBId, fixture.TodoCId, fixture.TodoAId }, todo.Cards.Select(card => card.Id));
        Assert.Equal(new[] { 0, 1, 2 }, todo.Cards.Select(card => card.Position));
    }

    [Fact]
    public async Task MoveCard_ToFirstPosition_PlacesCardAtTheTop()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var board = await fixture.Service.MoveCardAsync(
            fixture.BoardId,
            fixture.TodoCId,
            fixture.DoingId,
            newPosition: 0);

        var doing = board.Columns.Single(column => column.Id == fixture.DoingId);

        Assert.Equal(new[] { fixture.TodoCId, fixture.DoingAId, fixture.DoingBId }, doing.Cards.Select(card => card.Id));
        Assert.Equal(new[] { 0, 1, 2 }, doing.Cards.Select(card => card.Position));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-5)]
    public async Task MoveCard_PositionOutsideRange_IsClamped(int requestedPosition)
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var board = await fixture.Service.MoveCardAsync(
            fixture.BoardId,
            fixture.TodoAId,
            fixture.DoingId,
            requestedPosition);

        var doing = board.Columns.Single(column => column.Id == fixture.DoingId);
        var expectedIndex = requestedPosition < 0 ? 0 : doing.Cards.Count - 1;

        Assert.Equal(fixture.TodoAId, doing.Cards[expectedIndex].Id);
        Assert.Equal(new[] { 0, 1, 2 }, doing.Cards.Select(card => card.Position));
    }

    [Fact]
    public async Task MoveCard_ToColumnOfAnotherBoard_ThrowsAndLeavesTheBoardUntouched()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var otherBoard = new Board
        {
            Id = Guid.NewGuid(),
            Title = "Otro tablero",
            Columns =
            [
                new BoardColumn
                {
                    Id = Guid.NewGuid(),
                    Name = "Ajeno",
                    Order = 0,
                },
            ],
        };

        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();

        var foreignColumnId = otherBoard.Columns[0].Id;

        await Assert.ThrowsAsync<BoardNotFoundException>(() =>
            fixture.Service.MoveCardAsync(
                fixture.BoardId,
                fixture.TodoAId,
                foreignColumnId,
                newPosition: 0));

        var board = await fixture.Service.GetBoardAsync(fixture.BoardId);
        var todo = board!.Columns.Single(column => column.Id == fixture.TodoId);

        Assert.Equal(new[] { fixture.TodoAId, fixture.TodoBId, fixture.TodoCId }, todo.Cards.Select(card => card.Id));
        Assert.Equal(new[] { 0, 1, 2 }, todo.Cards.Select(card => card.Position));
    }

    [Fact]
    public async Task GetBoard_ReturnsColumnsAndCardsOrdered()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var board = await fixture.Service.GetBoardAsync(fixture.BoardId);

        Assert.NotNull(board);
        Assert.Equal(new[] { fixture.TodoId, fixture.DoingId, fixture.DoneId }, board!.Columns.Select(column => column.Id));
        Assert.Equal(new[] { fixture.TodoAId, fixture.TodoBId, fixture.TodoCId },
            board.Columns[0].Cards.Select(card => card.Id));
        Assert.Empty(board.Columns[2].Cards);
    }

    [Fact]
    public async Task CreateCard_AppendsAtEndOfColumn_WithNextPosition()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        var result = await fixture.Service.CreateCardAsync(
            fixture.DoingId,
            "  Nueva tarjeta  ",
            "  Con descripción  ");

        Assert.Equal("Nueva tarjeta", result.Card.Title);
        Assert.Equal("Con descripción", result.Card.Description);
        Assert.Equal(fixture.DoingId, result.Card.ColumnId);
        Assert.Equal(2, result.Card.Position);

        var doing = result.Board.Columns.Single(column => column.Id == fixture.DoingId);
        Assert.Equal(new[] { fixture.DoingAId, fixture.DoingBId, result.Card.Id }, doing.Cards.Select(card => card.Id));
    }

    [Fact]
    public async Task MoveCard_UnknownCard_Throws()
    {
        await using var fixture = await BoardFixture.CreateAsync();

        await Assert.ThrowsAsync<BoardNotFoundException>(() =>
            fixture.Service.MoveCardAsync(
                fixture.BoardId,
                Guid.NewGuid(),
                fixture.DoingId,
                newPosition: 0));
    }

    private sealed class BoardFixture : IAsyncDisposable
    {
        private BoardFixture(
            KanbelaDbContext db,
            Guid boardId,
            Guid todoId,
            Guid doingId,
            Guid doneId,
            Guid todoAId,
            Guid todoBId,
            Guid todoCId,
            Guid doingAId,
            Guid doingBId)
        {
            Db = db;
            Service = new BoardService(db);
            BoardId = boardId;
            TodoId = todoId;
            DoingId = doingId;
            DoneId = doneId;
            TodoAId = todoAId;
            TodoBId = todoBId;
            TodoCId = todoCId;
            DoingAId = doingAId;
            DoingBId = doingBId;
        }

        public KanbelaDbContext Db { get; }

        public BoardService Service { get; }

        public Guid BoardId { get; }

        public Guid TodoId { get; }

        public Guid DoingId { get; }

        public Guid DoneId { get; }

        public Guid TodoAId { get; }

        public Guid TodoBId { get; }

        public Guid TodoCId { get; }

        public Guid DoingAId { get; }

        public Guid DoingBId { get; }

        public static async Task<BoardFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<KanbelaDbContext>()
                .UseInMemoryDatabase($"kanbela-tests-{Guid.NewGuid()}")
                .Options;

            var db = new KanbelaDbContext(options);

            var boardId = Guid.NewGuid();
            var board = new Board { Id = boardId, Title = "Tablero de pruebas" };

            var todo = new BoardColumn { Id = Guid.NewGuid(), Name = "Por hacer", Order = 0, BoardId = boardId };
            var doing = new BoardColumn { Id = Guid.NewGuid(), Name = "En progreso", Order = 1, BoardId = boardId };
            var done = new BoardColumn { Id = Guid.NewGuid(), Name = "Hecho", Order = 2, BoardId = boardId };

            board.Columns.AddRange(todo, doing, done);

            var todoA = new Card { Id = Guid.NewGuid(), Title = "A", Position = 0, ColumnId = todo.Id };
            var todoB = new Card { Id = Guid.NewGuid(), Title = "B", Position = 1, ColumnId = todo.Id };
            var todoC = new Card { Id = Guid.NewGuid(), Title = "C", Position = 2, ColumnId = todo.Id };
            var doingA = new Card { Id = Guid.NewGuid(), Title = "D", Position = 0, ColumnId = doing.Id };
            var doingB = new Card { Id = Guid.NewGuid(), Title = "E", Position = 1, ColumnId = doing.Id };

            todo.Cards.AddRange(todoA, todoB, todoC);
            doing.Cards.AddRange(doingA, doingB);

            db.Boards.Add(board);
            await db.SaveChangesAsync();

            return new BoardFixture(
                db,
                boardId,
                todo.Id,
                doing.Id,
                done.Id,
                todoA.Id,
                todoB.Id,
                todoC.Id,
                doingA.Id,
                doingB.Id);
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}

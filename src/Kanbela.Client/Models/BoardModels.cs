namespace Kanbela.Client.Models;

public sealed class BoardSummaryDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int ColumnCount { get; set; }

    public int CardCount { get; set; }
}

public sealed class BoardDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<ColumnDto> Columns { get; set; } = [];
}

public sealed class ColumnDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public List<CardDto> Cards { get; set; } = [];
}

public sealed class CardDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Position { get; set; }

    public Guid ColumnId { get; set; }
}

public sealed record CreateCardInput(Guid ColumnId, string Title, string? Description);

public sealed record CardDropRequest(Guid ColumnId, int Position);

public sealed record CreateCardRequest(Guid ColumnId, string Title, string? Description);

public sealed record MoveCardRequest(Guid BoardId, Guid CardId, Guid TargetColumnId, int NewPosition);

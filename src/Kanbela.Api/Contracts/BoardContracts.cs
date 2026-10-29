namespace Kanbela.Api.Contracts;

public sealed record BoardSummaryDto(Guid Id, string Title, int ColumnCount, int CardCount);

public sealed record BoardDto(Guid Id, string Title, IReadOnlyList<ColumnDto> Columns);

public sealed record ColumnDto(Guid Id, string Name, int Order, IReadOnlyList<CardDto> Cards);

public sealed record CardDto(Guid Id, string Title, string? Description, int Position, Guid ColumnId);

/// <summary>Created card plus the board snapshot it belongs to.</summary>
public sealed record CardCreatedResult(CardDto Card, BoardDto Board);

public sealed record CreateCardRequest(Guid ColumnId, string? Title, string? Description);

public sealed record MoveCardRequest(Guid BoardId, Guid CardId, Guid TargetColumnId, int NewPosition);

namespace Kanbela.Api.Domain;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Zero-based position inside its column.</summary>
    public int Position { get; set; }

    public Guid ColumnId { get; set; }

    public BoardColumn? Column { get; set; }
}

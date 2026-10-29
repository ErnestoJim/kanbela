namespace Kanbela.Api.Domain;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public List<BoardColumn> Columns { get; set; } = [];
}

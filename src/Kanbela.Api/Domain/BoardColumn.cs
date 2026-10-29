namespace Kanbela.Api.Domain;

public class BoardColumn
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public Guid BoardId { get; set; }

    public Board? Board { get; set; }

    public List<Card> Cards { get; set; } = [];
}

namespace Kanbela.Api.Services;

/// <summary>Raised when a board, column or card cannot be found (or belongs to another board).</summary>
public sealed class BoardNotFoundException(string message) : Exception(message);

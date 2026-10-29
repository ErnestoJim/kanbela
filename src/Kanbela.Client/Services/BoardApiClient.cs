using System.Net.Http.Json;
using System.Text.Json;
using Kanbela.Client.Models;

namespace Kanbela.Client.Services;

public sealed class BoardApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BoardSummaryDto>> GetBoardsAsync(CancellationToken cancellationToken = default)
    {
        var boards = await http.GetFromJsonAsync<List<BoardSummaryDto>>("api/boards", JsonOptions, cancellationToken);
        return boards ?? [];
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"api/boards/{boardId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BoardDto>(JsonOptions, cancellationToken);
    }

    public async Task<CardDto> CreateCardAsync(
        Guid columnId,
        string title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
            "api/cards",
            new CreateCardRequest(columnId, title, description),
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CardDto>(JsonOptions, cancellationToken)
            ?? throw new BoardApiException("La API devolvió una tarjeta vacía.");
    }

    public async Task<BoardDto> MoveCardAsync(
        Guid boardId,
        Guid cardId,
        Guid targetColumnId,
        int newPosition,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PutAsJsonAsync(
            "api/cards/move",
            new MoveCardRequest(boardId, cardId, targetColumnId, newPosition),
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<BoardDto>(JsonOptions, cancellationToken)
            ?? throw new BoardApiException("La API devolvió un tablero vacío.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new BoardApiException(
            $"La API respondió {(int)response.StatusCode} ({response.ReasonPhrase}). {body}".Trim());
    }
}

public sealed class BoardApiException(string message) : Exception(message);

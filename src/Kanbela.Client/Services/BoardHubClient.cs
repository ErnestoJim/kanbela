using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kanbela.Client.Models;

namespace Kanbela.Client.Services;

/// <summary>
/// Long-lived SignalR connection for the board page. Keeps the current board
/// subscription across automatic reconnects.
/// </summary>
public sealed class BoardHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<BoardHubClient> _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private Guid? _currentBoardId;

    public BoardHubClient(IConfiguration configuration, ILogger<BoardHubClient> logger)
    {
        _logger = logger;

        var apiBaseUrl = (configuration["ApiBaseUrl"] ?? "http://localhost:5080").TrimEnd('/');

        _connection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/hubs/board")
            .WithAutomaticReconnect(
            [
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
            ])
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .Build();

        _connection.On<BoardDto>("BoardUpdated", board =>
            BoardUpdated?.Invoke(board) ?? Task.CompletedTask);

        _connection.Reconnected += async _ =>
        {
            try
            {
                await JoinCurrentBoardAsync();
                _logger.LogInformation("Conexión SignalR restablecida.");
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "No se pudo volver a unir al tablero tras reconectar.");
            }

            if (Reconnected is not null)
            {
                await Reconnected.Invoke();
            }
        };

        _connection.Closed += exception =>
        {
            ConnectionClosed?.Invoke(exception);
            return Task.CompletedTask;
        };
    }

    /// <summary>Raised for every board snapshot pushed by the server.</summary>
    public event Func<BoardDto, Task>? BoardUpdated;

    /// <summary>Raised after SignalR automatically reconnects.</summary>
    public event Func<Task>? Reconnected;

    /// <summary>Raised when the connection is closed and cannot reconnect.</summary>
    public event Action<Exception?>? ConnectionClosed;

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public async Task ConnectAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        _currentBoardId = boardId;
        await EnsureStartedAsync(cancellationToken);
        await _connection.InvokeAsync("JoinBoard", boardId.ToString(), cancellationToken);
    }

    public async Task LeaveBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        if (_currentBoardId == boardId)
        {
            _currentBoardId = null;
        }

        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveBoard", boardId.ToString(), cancellationToken);
        }
    }

    public Task MoveCardAsync(
        Guid boardId,
        Guid cardId,
        Guid targetColumnId,
        int newPosition,
        CancellationToken cancellationToken = default)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("La conexión en tiempo real no está disponible.");
        }

        return _connection.InvokeAsync(
            "MoveCard",
            boardId.ToString(),
            cardId,
            targetColumnId,
            newPosition,
            cancellationToken);
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken);
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task JoinCurrentBoardAsync()
    {
        if (_currentBoardId is { } boardId && _connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinBoard", boardId.ToString());
        }
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

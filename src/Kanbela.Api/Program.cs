using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Kanbela.Api.Data;
using Kanbela.Api.Endpoints;
using Kanbela.Api.Hubs;
using Kanbela.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Persistence: SQLite file under App_Data (created on first run).
// ---------------------------------------------------------------------------
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var databasePath = Path.Combine(dataDirectory, "kanbela.db");
var connectionString = builder.Configuration.GetConnectionString("KanbelaDb")
    ?? $"Data Source={databasePath}";

builder.Services.AddDbContext<KanbelaDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IBoardService, BoardService>();

// ---------------------------------------------------------------------------
// Real-time: SignalR with camelCase payloads shared with the Blazor client.
// ---------------------------------------------------------------------------
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Open CORS policy for local development (API :5080, client :5081).
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database creation + demo data.
// ---------------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KanbelaDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db);

    app.Logger.LogInformation(
        "Tablero de demostración disponible en /api/boards/{BoardId}",
        DataSeeder.DemoBoardId);
}

app.UseExceptionHandler();

app.UseCors("dev");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapBoardEndpoints();
app.MapHub<BoardHub>("/hubs/board");

app.Run();

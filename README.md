# Kanbela · Kanban colaborativo en tiempo real

Aplicación estilo Kanban de extremo a extremo construida con **.NET 9**, **ASP.NET Core Minimal APIs**, **Blazor WebAssembly**, **Entity Framework Core (SQLite)** y **SignalR**.

Puedes abrir el mismo tablero en dos navegadores y arrastrar tarjetas: los cambios se persisten y se reflejan al instante en la otra sesión.

---

## 1. Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (los proyectos tienen `TargetFramework` `net9.0`).
- Un navegador moderno.

> El repositorio no necesita base de datos previa: SQLite se crea sola en `src/Kanbela.Api/App_Data/kanbela.db` y se siembra el tablero de demostración en el primer arranque.

---

## 2. Estructura de la solución

```text
Kanbela.sln
├── src/
│   ├── Kanbela.Api/                 ASP.NET Core Minimal API + SignalR + EF Core
│   │   ├── Contracts/                 DTOs de la API
│   │   ├── Data/                      DbContext y DataSeeder
│   │   ├── Domain/                    Board, BoardColumn, Card
│   │   ├── Endpoints/                 Endpoints REST (/api/...)
│   │   ├── Hubs/                      BoardHub (SignalR)
│   │   ├── Services/                  BoardService (movimiento y reordenamiento)
│   │   └── Program.cs                 Arranque, CORS, SQLite y seed
│   └── Kanbela.Client/              Blazor WebAssembly
│       ├── Components/                BoardColumnView, AddCardModal
│       ├── Models/                    DTOs del cliente
│       ├── Pages/                     Home y Board (/board/{boardId})
│       ├── Services/                  BoardApiClient y BoardHubClient
│       └── wwwroot/js/dnd.js          Cálculo del índice de drop (HTML5 DnD)
└── tests/
    └── Kanbela.Tests/               xUnit + EF Core InMemory
```

---

## 3. Ejecutar API y cliente a la vez

Abre **dos terminales** en la raíz del repositorio.

### Terminal 1 — API

```bash
dotnet run --project src/Kanbela.Api
```

- Escucha en `http://localhost:5080`
- Health check: `http://localhost:5080/health`
- Tablero de demostración: `http://localhost:5080/api/boards/8a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d`

### Terminal 2 — Cliente Blazor

```bash
dotnet run --project src/Kanbela.Client
```

- Abre `http://localhost:5081`
- La página principal carga el primer tablero y navega a `/board/{boardId}`.

### Alternativa en una sola terminal (macOS/Linux)

```bash
dotnet run --project src/Kanbela.Api & \
dotnet run --project src/Kanbela.Client
```

Para detener ambos procesos: `kill %1 %2` (o `Ctrl+C` y detener el proceso restante).

> Los puertos se configuran en `src/Kanbela.Api/Properties/launchSettings.json` y `src/Kanbela.Client/Properties/launchSettings.json`. Si cambias el de la API, actualiza `src/Kanbela.Client/wwwroot/appsettings.json` (`ApiBaseUrl`).

---

## 4. Cómo probar el tiempo real

1. Abre `http://localhost:5081` en una ventana normal.
2. Abre la misma URL en una ventana de incógnito (o en otro navegador) para simular a otro usuario.
3. Arrastra una tarjeta entre columnas o crea una tarjeta con el botón **+ Nueva tarjeta**.
4. La otra ventana se actualiza sin recargar. El indicador **En tiempo real** confirma la conexión a SignalR.

Si SignalR no está disponible, el cliente sigue funcionando en modo REST y muestra el aviso **Sin tiempo real · Reintentar**.

---

## 5. API REST

| Método | Ruta | Descripción |
| --- | --- | --- |
| `GET` | `/api/boards` | Lista de tableros con conteos. |
| `GET` | `/api/boards/{id}` | Tablero con columnas y tarjetas ordenadas por `Order` y `Position`. |
| `POST` | `/api/cards` | Crea una tarjeta al final de la columna indicada. |
| `PUT` | `/api/cards/move` | Mueve y/o reordena una tarjeta. |

### Crear una tarjeta

```http
POST /api/cards
Content-Type: application/json

{
  "columnId": "11111111-1111-4111-8111-111111111111",
  "title": "Revisar el README",
  "description": "Opcional"
}
```

### Mover una tarjeta

```http
PUT /api/cards/move
Content-Type: application/json

{
  "boardId": "8a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  "cardId": "aaaaaaa1-aaaa-4aaa-8aaa-aaaaaaaaaaa1",
  "targetColumnId": "22222222-2222-4222-8222-222222222222",
  "newPosition": 0
}
```

`newPosition` es un índice **basado en cero** y se recorta al rango válido (`0..cantidadDeTarjetas`). El servicio elimina la tarjeta de su columna, la inserta en el índice pedido y renumera ambas columnas para que las posiciones queden contiguas.

También encontrarás ejemplos ejecutables en `src/Kanbela.Api/Kanbela.Api.http`.

---

## 6. SignalR

Hub: `http://localhost:5080/hubs/board` (grupo `board-{boardId}`).

| Método del hub | Descripción |
| --- | --- |
| `JoinBoard(string boardId)` | Une la conexión al grupo del tablero. |
| `LeaveBoard(string boardId)` | Sale del grupo. |
| `MoveCard(string boardId, Guid cardId, Guid targetColumnId, int newPosition)` | Persiste el movimiento y notifica a `Clients.OthersInGroup(...)`. |

Evento del servidor:

- `BoardUpdated(BoardDto)`: snapshot completo del tablero. Se emite a los demás clientes del grupo al mover una tarjeta desde el hub y a todo el grupo cuando una tarjeta se crea o se mueve por REST. El cliente reemplaza su estado con el snapshot, de modo que la UI converge sin recargar.

El cliente aplica el movimiento de forma optimista y usa la API REST como respaldo si el hub no está conectado. La reconexión automática vuelve a unirse al grupo y resincroniza el tablero.

---

## 7. Modelo de datos

- `Board`: `Id`, `Title`, `Columns`.
- `BoardColumn`: `Id`, `Name`, `Order`, `BoardId`, `Cards`.
- `Card`: `Id`, `Title`, `Description`, `Position`, `ColumnId`.

Seed inicial (`DataSeeder`): tablero **"Mi Proyecto"** con las columnas **"Por hacer"**, **"En progreso"** y **"Hecho"**, y dos tarjetas de ejemplo. El Id del tablero es fijo (`8a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d`) para poder enlazarlo directamente.

Para reiniciar los datos, detén la API y elimina `src/Kanbela.Api/App_Data/kanbela.db`; se volverá a crear y sembrar en el siguiente arranque.

---

## 8. Tests y build

```bash
dotnet build
dotnet test
```

El proyecto `Kanbela.Tests` usa xUnit y EF Core InMemory. Cubre:

- Movimiento entre columnas con inserción en el índice pedido y renumeración de origen y destino.
- Reordenamiento dentro de la misma columna.
- Inserción al inicio.
- Recorte de posiciones fuera de rango (negativas o mayores que el total).
- Rechazo de movimientos hacia columnas de otro tablero.
- Orden de columnas y tarjetas en la consulta del tablero.
- Creación de tarjetas al final de la columna con la siguiente posición.
- Tarjeta inexistente.

> El proyecto de tests define `RollForward=LatestMajor`, de modo que `dotnet test` también funciona en equipos que solo tienen instalado un runtime más reciente. Con .NET 9 instalado se ejecuta sobre .NET 9.

---

## 9. Decisiones de implementación

- **Posiciones contiguas**: cada movimiento normaliza `Position` a `0..n-1` en la columna destino y, si cambió de columna, también en la de origen. Esto evita huecos y hace determinista el orden.
- **Un único algoritmo**: mover dentro de la misma columna y entre columnas usa la misma operación (quitar + insertar + renumerar).
- **Concurrencia**: el hub y el endpoint REST comparten `BoardService`; cada operación se guarda con EF Core y se difunde el snapshot resultante.
- **DnD nativo**: se usan eventos HTML5 (`dragstart`, `dragover`, `drop`) de Blazor. Un helper JS mínimo calcula el índice según la posición del puntero y el punto medio de las tarjetas; el `dataTransfer` se inicializa para compatibilidad con Firefox.
- **CORS abierto** en desarrollo para `http://localhost:5081` y cualquier origen local.

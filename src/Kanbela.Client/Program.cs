using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Kanbela.Client;
using Kanbela.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080").TrimEnd('/');

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri($"{apiBaseUrl}/") });
builder.Services.AddScoped<BoardApiClient>();
builder.Services.AddSingleton<BoardHubClient>();

await builder.Build().RunAsync();

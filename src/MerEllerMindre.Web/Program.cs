using System.Text.Encodings.Web;
using System.Text.Unicode;
using MerEllerMindre.Web;
using MerEllerMindre.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Render Swedish letters (å/ä/ö) and units (km²) literally instead of numeric HTML entities.
// Static SSR uses the registered HtmlEncoder; the default only allows Basic Latin.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// File-based Quiz catalog: load every data/packs/*.csv at startup (fail-fast). The packs
// are domain reference data copied to output beside the Domain assembly (see its csproj).
var packsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "packs");
builder.Services.AddSingleton(new FileSystemQuestionPackCatalog(packsDirectory));

// Razor Components in STATIC SSR (no interactive render mode → no circuit, no WebSocket,
// no blazor.web.js). Endpoints return RazorComponentResult<T>. See ADR 007.
builder.Services.AddRazorComponents();

// Imperative-shell game state: a single in-memory event log shared across requests, with the
// repository (joinCode→gameId index) and the command-side application service on top.
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<GameRepository>();
builder.Services.AddSingleton<GameApplicationService>();

// Per-game player identity in an encrypted cookie.
builder.Services.AddDataProtection();
builder.Services.AddSingleton<PlayerIdentity>();

// Antiforgery for the catalog POST form (htmx posts the hidden token field).
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

// Browsers auto-request /favicon.ico; answer 204 so it doesn't surface as a stray 404.
app.MapGet("/favicon.ico", () => Results.NoContent());

app.MapGameEndpoints();

app.Run();

// Exposed so the WebApplicationFactory integration tests can reference the entry-point assembly.
public partial class Program;

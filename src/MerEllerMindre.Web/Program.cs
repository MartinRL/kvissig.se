using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
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

// Logo corpus for the loggor-* packs: maps a pack's company name → its PNG URL (served at
// /logos below), skipping rows whose PNG isn't on disk so a missing download never breaks.
var logosDirectory = Path.Combine(AppContext.BaseDirectory, "data", "logos");
builder.Services.AddSingleton(new LogoCatalog(logosDirectory));

// Blindbudet (sealed-bid auction) lot catalog — a SEPARATE directory from the quiz packs so
// the two games' decks never mix. Loaded fail-fast at startup, same CSV-catalog convention.
var auctionPacksDirectory = Path.Combine(AppContext.BaseDirectory, "data", "auction-packs");
builder.Services.AddSingleton(new FileSystemAuctionPackCatalog(auctionPacksDirectory));
builder.Services.AddSingleton<AuctionApplicationService>();

// Razor Components in STATIC SSR (no interactive render mode → no circuit, no WebSocket,
// no blazor.web.js). Endpoints return RazorComponentResult<T>. See ADR 007.
builder.Services.AddRazorComponents();

// MainLayout reads the request host to build absolute canonical/OG URLs.
builder.Services.AddHttpContextAccessor();

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

// Plausible Events API client for the server-side gameplay funnel (no-op outside Production).
builder.Services.AddHttpClient<PlausibleClient>();

var app = builder.Build();

// Behind fly's edge proxy: honour X-Forwarded-Proto/-For so the app sees https
// (secure cookies + antiforgery). fly terminates TLS and forwards plain http internally.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

// ponytail: ReExecute återanvänder MainLayout — ingen separat 404-pipeline.
app.UseStatusCodePagesWithReExecute("/404");

app.UseStaticFiles();

// Serve the logo PNGs from the Domain's data/logos corpus at /logos/{origin}/{slug}.png.
if (Directory.Exists(logosDirectory))
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(logosDirectory),
        RequestPath = "/logos",
    });

app.UseAntiforgery();

// Liveness probe for fly's health check.
app.MapGet("/healthz", () => Results.Ok("ok"));

app.MapGameEndpoints();
app.MapAuctionEndpoints();

app.Run();

// Exposed so the WebApplicationFactory integration tests can reference the entry-point assembly.
public partial class Program;

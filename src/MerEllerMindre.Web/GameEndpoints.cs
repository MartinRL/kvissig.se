using System.Globalization;
using MerEllerMindre.Domain;
using MerEllerMindre.Web.Components;
using MerEllerMindre.Web.Components.Screens;
using MerEllerMindre.Web.Infrastructure;
using MerEllerMindre.Web.Presentation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using DomainOk = MerEllerMindre.Domain.Ok<MerEllerMindre.Domain.GameEvent[]>;

namespace MerEllerMindre.Web;

/// <summary>
/// The DI services every game handler needs, bundled so handlers bind them with one
/// <c>[AsParameters]</c> parameter instead of five. ASP.NET resolves each member from DI.
/// </summary>
public record GameDeps(
    GameApplicationService Svc,
    PlayerIdentity Identity,
    LogoCatalog Logos,
    PlausibleClient Plausible,
    IAntiforgery Antiforgery);

/// <summary>Everything a screen fragment needs to render, so RenderState takes one argument.</summary>
public record RenderContext(
    GameState State,
    Guid? Viewer,
    string Token,
    string JoinUrl,
    Func<string, string?> ResolveLogo,
    bool ShowJoinUrl = false);

/// <summary>
/// All game HTTP endpoints. POSTs require the antiforgery token (htmx posts the hidden form
/// field); GET pages mint + store it. Player identity is a per-game encrypted cookie. Screens
/// are returned as RazorComponentResult fragments (static SSR) swapped into #screen by htmx;
/// the create/join POSTs answer with HX-Redirect to the lobby shell instead.
/// </summary>
public static class GameEndpoints
{
    // Web-only "new pack" markers: drop a slug here when its deck stops being new.
    static readonly HashSet<string> NewPacks = new(StringComparer.Ordinal)
        { "loggor-mini-1", "hundraser-mini", "elbil-mini", "fotboll-mini" };

    public static void MapGameEndpoints(this WebApplication app)
    {
        app.MapGet("/", GetCatalog);
        app.MapGet("/om-spelet", () => new RazorComponentResult<OmSpelet>());
        app.MapGet("/om-mig", () => new RazorComponentResult<OmMig>());
        app.MapGet("/spel-som-0-100", () => new RazorComponentResult<SpelSom0100>());
        app.MapGet("/lev-som-du-lar", () => new RazorComponentResult<LevSomDuLar>());
        app.MapGet("/fragespel-online", () => new RazorComponentResult<FragespelOnline>());
        app.MapGet("/spel-som-more-or-less", () => new RazorComponentResult<SpelSomMoreOrLess>());
        app.MapGet("/hund-fragesport-tillsammans", () => new RazorComponentResult<HundFragesport>());
        app.MapGet("/elbil-fragesport-tillsammans", () => new RazorComponentResult<ElbilFragesport>());
        app.MapGet("/fotboll-fragesport-tillsammans", () => new RazorComponentResult<FotbollFragesport>());
        app.MapGet("/404", () => new RazorComponentResult<Components.NotFound>());
        app.MapGet("/sitemap.xml", GetSitemap);
        app.MapGet("/games/new/{packId}", GetNewGame);
        app.MapPost("/games", PostGame);
        app.MapGet("/games/{code}/join", GetJoin);
        app.MapPost("/games/{code}/join", PostJoin);
        app.MapGet("/games/{code}", GetGame);
        app.MapGet("/games/{code}/state", GetState);
        app.MapPost("/games/{code}/start", PostStart);
        app.MapPost("/games/{code}/direction", PostDirection);
        app.MapGet("/games/{code}/difference", GetDifference);
        app.MapPost("/games/{code}/difference", PostDifference);
        app.MapPost("/games/{code}/next", PostNext);
    }

    private static IResult GetCatalog(FileSystemQuestionPackCatalog catalog)
    {
        var packs = catalog.Packs
            .Select(p => new PackVm(p.PackId, p.Name, p.QuestionCount, NewPacks.Contains(p.PackId)))
            .OrderBy(p => p.PackId switch
            {
                "familj" => 0,
                "alla-aldrar" => 1,
                "mer-eller-mindre" => 2,
                _ => p.IsNew ? 3 : 4,
            })
            .ToList();
        return new RazorComponentResult<QuizCatalog>(new { Model = new CatalogVm(packs) });
    }

    private static IResult GetSitemap(FileSystemQuestionPackCatalog catalog, HttpContext http)
    {
        var root = $"{http.Request.Scheme}://{http.Request.Host}";
        var urls = new List<string> { "/", "/om-spelet", "/om-mig", "/spel-som-0-100", "/lev-som-du-lar", "/fragespel-online", "/spel-som-more-or-less", "/hund-fragesport-tillsammans", "/elbil-fragesport-tillsammans", "/fotboll-fragesport-tillsammans", "/tank-till-tusen" };
        urls.AddRange(catalog.Packs.Select(p => $"/games/new/{p.PackId}"));
        var body = string.Concat(urls.Select(u =>
            $"<url><loc>{System.Security.SecurityElement.Escape(root + u)}</loc></url>"));
        var xml = $"""<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">{body}</urlset>""";
        return Results.Text(xml, "application/xml");
    }

    private static IResult GetNewGame(string packId, FileSystemQuestionPackCatalog catalog, IAntiforgery antiforgery, HttpContext http)
    {
        if (catalog.Find(packId) is not { } pack)
            return Results.NotFound("Frågepaketet hittades inte.");

        var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
        return new RazorComponentResult<HostForm>(new { Model = new HostFormVm(pack.PackId, pack.Name, token) });
    }

    private static IResult PostGame([FromForm] string questionPackId, [FromForm] string hostName, [AsParameters] GameDeps d, HttpContext http)
    {
        var result = d.Svc.Open(new OpenLobby(hostName, questionPackId));
        if (result is not DomainOk ok || ok.Value is not [LobbyOpened opened, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Plausible.Track("game_created", http);
        d.Identity.SetPlayer(http, opened.GameId, opened.HostPlayerId);
        http.Response.Headers["HX-Redirect"] = $"/games/{opened.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetJoin(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Spelet hittades inte.");

        var token = d.Antiforgery.GetAndStoreTokens(http).RequestToken!;
        var hostName = state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        return new RazorComponentResult<JoinForm>(new { Model = new JoinVm(state.JoinCode, hostName, token) });
    }

    private static IResult PostJoin(string code, [FromForm] string playerName, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Spelet hittades inte.");

        var result = d.Svc.Execute(gameId, new JoinGame(state.JoinCode, playerName));
        if (result is not DomainOk ok || ok.Value is not [PlayerJoined joined, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Plausible.Track("player_joined", http);
        d.Identity.SetPlayer(http, gameId, joined.PlayerId);
        http.Response.Headers["HX-Redirect"] = $"/games/{state.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetGame(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Spelet hittades inte.");
        return new RazorComponentResult<GameShell>(new { JoinCode = state.JoinCode, ShowJoinUrl = http.Request.Query.ContainsKey("url") });
    }

    private static IResult GetState(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Spelet hittades inte.");
        return Rendered(state, d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostStart(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        if (d.Svc.Execute(gameId, new StartGame(gameId)) is DomainOk { Value: [GameStarted, ..] })
            d.Plausible.Track("game_started", http);
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostDirection(string code, [FromForm] string direction, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        var viewer = d.Identity.GetPlayer(http, gameId);
        if (viewer is { } playerId && Enum.TryParse<Direction>(direction, out var dir))
        {
            d.Svc.Execute(gameId, new SubmitDirection(gameId, playerId, dir));
            d.Svc.RunRevealDirectionGear(gameId);
        }
        return Rendered(d.Svc.Load(gameId), viewer, d, http);
    }

    // Mellansteg → slider: render the stage-2 question once the direction is revealed.
    private static IResult GetDifference(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Spelet hittades inte.");

        if (!ReadyForDifference(state))
            return Rendered(state, viewer: null, d, http);

        var token = d.Antiforgery.GetAndStoreTokens(http).RequestToken!;
        return new RazorComponentResult<QuestionScreen>(new { Model = GameScreens.Question(state, token, QuestionStage.Difference, d.Logos.UrlFor) });
    }

    // The stage-2 slider is only valid on a started game whose direction is revealed but not yet scored.
    private static bool ReadyForDifference(GameState state)
    {
        var i = state.CurrentQuestionIndex;
        return state.Phase == GamePhase.Started && state.DirectionRevealed(i) && !state.Questions[i].Scored;
    }

    private static IResult PostDifference(string code, [FromForm] string guessedDifference, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        var viewer = d.Identity.GetPlayer(http, gameId);
        if (viewer is { } playerId
            && decimal.TryParse(guessedDifference, NumberStyles.Number, CultureInfo.InvariantCulture, out var diff))
        {
            d.Svc.Execute(gameId, new SubmitDifference(gameId, playerId, diff));
            d.Svc.RunScoreDifferenceGear(gameId);
        }
        return Rendered(d.Svc.Load(gameId), viewer, d, http);
    }

    private static IResult PostNext(string code, [AsParameters] GameDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        if (d.Svc.RunProgressionGear(gameId) is DomainOk { Value: var events } && events.Any(e => e is GameEnded))
            d.Plausible.Track("game_completed", http);
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    /// <summary>
    /// The absolute join URL the host's QR encodes. Behind fly's edge ForwardedHeaders makes
    /// Scheme=https and Host the public hostname, so the QR is scannable over the internet.
    /// </summary>
    private static string AbsoluteJoinUrl(HttpContext http, Guid joinCode) =>
        $"{http.Request.Scheme}://{http.Request.Host}/games/{joinCode:N}/join";

    private static IResult Rendered(GameState state, Guid? viewer, GameDeps d, HttpContext http) =>
        RenderState(new RenderContext(
            state, viewer,
            d.Antiforgery.GetAndStoreTokens(http).RequestToken!,
            AbsoluteJoinUrl(http, state.JoinCode),
            d.Logos.UrlFor,
            http.Request.Query.ContainsKey("url")));

    private static IResult RenderState(RenderContext c) =>
        GameScreens.Select(c.State, c.Viewer) switch
        {
            ScreenKind.LobbyHost => new RazorComponentResult<LobbyHostScreen>(new { Model = GameScreens.Lobby(c.State, c.Viewer, c.Token, c.JoinUrl, c.ShowJoinUrl) }),
            ScreenKind.LobbyPlayer => new RazorComponentResult<LobbyPlayerScreen>(new { Model = GameScreens.Lobby(c.State, c.Viewer, c.Token, c.JoinUrl, showJoinUrl: false) }),
            ScreenKind.Question => new RazorComponentResult<QuestionScreen>(new { Model = GameScreens.Question(c.State, c.Token, QuestionStage.Direction, c.ResolveLogo) }),
            ScreenKind.Waiting => new RazorComponentResult<WaitingScreen>(new { Model = GameScreens.Waiting(c.State, c.Viewer) }),
            ScreenKind.DirectionResults => new RazorComponentResult<DirectionResultsScreen>(new { Model = GameScreens.DirectionResults(c.State, c.Viewer, c.ResolveLogo) }),
            ScreenKind.Results => new RazorComponentResult<ResultsScreen>(new { Model = GameScreens.Results(c.State, c.Viewer, c.Token, c.ResolveLogo) }),
            ScreenKind.Standings => new RazorComponentResult<StandingsScreen>(new { Model = GameScreens.Standings(c.State, c.Viewer) }),
            _ => throw new InvalidOperationException($"Unhandled screen kind for game {c.State.GameId}.")
        };

    private static (Guid GameId, GameState State)? Resolve(GameApplicationService svc, string code)
    {
        if (!Guid.TryParse(code, out var joinCode))
            return null;
        var gameId = svc.ResolveJoinCode(joinCode);
        return gameId is null ? null : (gameId.Value, svc.Load(gameId.Value));
    }

    private static string Describe(GameError error) => error switch
    {
        GameNotFound => "Spelet hittades inte.",
        GameAlreadyStarted => "Omgången har redan startat.",
        NameAlreadyTaken => "Namnet är upptaget.",
        NotEnoughPlayers => "Det behövs minst 2 spelare.",
        QuestionPackNotFound => "Frågepaketet hittades inte.",
        GameNotStarted => "Omgången har inte startat.",
        PlayerNotInGame => "Du är inte med i spelet.",
        AlreadySubmittedDirection => "Du har redan svarat mer eller mindre.",
        AlreadySubmittedDifference => "Du har redan gissat skillnaden.",
        DirectionNotRevealed => "Mer eller mindre är inte avslöjat än.",
        DifferenceOutOfRange => "Ogiltig gissning.",
        NotAllDirectionsIn => "Alla har inte svarat mer eller mindre än.",
        DirectionAlreadyRevealed => "Mer eller mindre är redan avslöjat.",
        NotAllDifferencesIn => "Alla har inte gissat än.",
        QuestionAlreadyScored => "Frågan är redan rättad."
    };
}

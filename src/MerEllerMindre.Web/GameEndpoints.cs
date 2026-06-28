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
        app.MapGet("/", (FileSystemQuestionPackCatalog catalog) =>
        {
            var packs = catalog.Packs
                .Select(p => new PackVm(p.PackId, p.Name, p.QuestionCount, NewPacks.Contains(p.PackId)))
                .OrderBy(p => p.PackId == "mer-eller-mindre" ? 0 : p.IsNew ? 1 : 2)
                .ToList();
            return new RazorComponentResult<QuizCatalog>(new { Model = new CatalogVm(packs) });
        });

        app.MapGet("/om-spelet", () => new RazorComponentResult<OmSpelet>());

        app.MapGet("/om-mig", () => new RazorComponentResult<OmMig>());

        app.MapGet("/spel-som-0-100", () => new RazorComponentResult<SpelSom0100>());

        app.MapGet("/fragespel-online", () => new RazorComponentResult<FragespelOnline>());

        app.MapGet("/spel-som-more-or-less", () => new RazorComponentResult<SpelSomMoreOrLess>());

        app.MapGet("/hund-fragesport-tillsammans", () => new RazorComponentResult<HundFragesport>());

        app.MapGet("/elbil-fragesport-tillsammans", () => new RazorComponentResult<ElbilFragesport>());

        app.MapGet("/fotboll-fragesport-tillsammans", () => new RazorComponentResult<FotbollFragesport>());

        app.MapGet("/404", () => new RazorComponentResult<Components.NotFound>());

        app.MapGet("/sitemap.xml", (FileSystemQuestionPackCatalog catalog, HttpContext http) =>
        {
            var root = $"{http.Request.Scheme}://{http.Request.Host}";
            var urls = new List<string> { "/", "/om-spelet", "/om-mig", "/spel-som-0-100", "/fragespel-online", "/spel-som-more-or-less", "/hund-fragesport-tillsammans", "/elbil-fragesport-tillsammans", "/fotboll-fragesport-tillsammans" };
            urls.AddRange(catalog.Packs.Select(p => $"/games/new/{p.PackId}"));
            var body = string.Concat(urls.Select(u =>
                $"<url><loc>{System.Security.SecurityElement.Escape(root + u)}</loc></url>"));
            var xml = $"""<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">{body}</urlset>""";
            return Results.Text(xml, "application/xml");
        });

        app.MapGet("/games/new/{packId}", (string packId, FileSystemQuestionPackCatalog catalog, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (catalog.Find(packId) is not { } pack)
                return Results.NotFound("Frågepaketet hittades inte.");

            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            return new RazorComponentResult<HostForm>(new { Model = new HostFormVm(pack.PackId, pack.Name, token) });
        });

        app.MapPost("/games", (
            [FromForm] string questionPackId,
            [FromForm] string hostName,
            GameApplicationService svc,
            PlayerIdentity identity,
            PlausibleClient plausible,
            HttpContext http) =>
        {
            var result = svc.Open(new OpenLobby(hostName, questionPackId));
            if (result is not DomainOk ok || ok.Value is not [LobbyOpened opened, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

            plausible.Track("game_created", http);
            identity.SetPlayer(http, opened.GameId, opened.HostPlayerId);
            http.Response.Headers["HX-Redirect"] = $"/games/{opened.JoinCode:N}";
            return Results.Ok();
        });

        app.MapGet("/games/{code}/join", (string code, GameApplicationService svc, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (_, state))
                return Results.NotFound("Spelet hittades inte.");

            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            var hostName = state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
            return new RazorComponentResult<JoinForm>(new { Model = new JoinVm(state.JoinCode, hostName, token) });
        });

        app.MapPost("/games/{code}/join", (
            string code,
            [FromForm] string playerName,
            GameApplicationService svc,
            PlayerIdentity identity,
            PlausibleClient plausible,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");

            var result = svc.Execute(gameId, new JoinGame(state.JoinCode, playerName));
            if (result is not DomainOk ok || ok.Value is not [PlayerJoined joined, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

            plausible.Track("player_joined", http);
            identity.SetPlayer(http, gameId, joined.PlayerId);
            http.Response.Headers["HX-Redirect"] = $"/games/{state.JoinCode:N}";
            return Results.Ok();
        });

        app.MapGet("/games/{code}", (string code, GameApplicationService svc, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (_, state))
                return Results.NotFound("Spelet hittades inte.");
            return new RazorComponentResult<GameShell>(new { JoinCode = state.JoinCode, ShowJoinUrl = http.Request.Query.ContainsKey("url") });
        });

        app.MapGet("/games/{code}/state", (string code, GameApplicationService svc, PlayerIdentity identity, LogoCatalog logos, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode), logos.UrlFor, http.Request.Query.ContainsKey("url"));
        });

        app.MapPost("/games/{code}/start", (string code, GameApplicationService svc, PlayerIdentity identity, LogoCatalog logos, PlausibleClient plausible, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            if (svc.Execute(gameId, new StartGame(gameId)) is DomainOk { Value: [GameStarted, ..] })
                plausible.Track("game_started", http);
            var state = svc.Load(gameId);
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode), logos.UrlFor);
        });

        app.MapPost("/games/{code}/direction", (
            string code,
            [FromForm] string direction,
            GameApplicationService svc,
            PlayerIdentity identity,
            LogoCatalog logos,
            IAntiforgery antiforgery,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            var viewer = identity.GetPlayer(http, gameId);
            if (viewer is { } playerId && Enum.TryParse<Direction>(direction, out var dir))
            {
                svc.Execute(gameId, new SubmitDirection(gameId, playerId, dir));
                svc.RunRevealDirectionGear(gameId);
            }

            var next = svc.Load(gameId);
            return RenderState(next, viewer, antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, next.JoinCode), logos.UrlFor);
        });

        // Mellansteg → slider: render the stage-2 question once the direction is revealed.
        app.MapGet("/games/{code}/difference", (string code, GameApplicationService svc, LogoCatalog logos, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");

            var i = state.CurrentQuestionIndex;
            if (state.Phase != GamePhase.Started || !state.DirectionRevealed(i) || state.Questions[i].Scored)
                return RenderState(state, viewer: null, antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode), logos.UrlFor);

            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            return new RazorComponentResult<QuestionScreen>(new { Model = GameScreens.Question(state, token, QuestionStage.Difference, logos.UrlFor) });
        });

        app.MapPost("/games/{code}/difference", (
            string code,
            [FromForm] string guessedDifference,
            GameApplicationService svc,
            PlayerIdentity identity,
            LogoCatalog logos,
            IAntiforgery antiforgery,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            var viewer = identity.GetPlayer(http, gameId);
            if (viewer is { } playerId
                && decimal.TryParse(guessedDifference, NumberStyles.Number, CultureInfo.InvariantCulture, out var diff))
            {
                svc.Execute(gameId, new SubmitDifference(gameId, playerId, diff));
                svc.RunScoreDifferenceGear(gameId);
            }

            var scored = svc.Load(gameId);
            return RenderState(scored, viewer, antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, scored.JoinCode), logos.UrlFor);
        });

        app.MapPost("/games/{code}/next", (string code, GameApplicationService svc, PlayerIdentity identity, LogoCatalog logos, PlausibleClient plausible, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            if (svc.RunProgressionGear(gameId) is DomainOk { Value: var events } && events.Any(e => e is GameEnded))
                plausible.Track("game_completed", http);
            var state = svc.Load(gameId);
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode), logos.UrlFor);
        });
    }

    /// <summary>
    /// The absolute join URL the host's QR encodes. Behind fly's edge ForwardedHeaders makes
    /// Scheme=https and Host the public hostname, so the QR is scannable over the internet.
    /// </summary>
    private static string AbsoluteJoinUrl(HttpContext http, Guid joinCode) =>
        $"{http.Request.Scheme}://{http.Request.Host}/games/{joinCode:N}/join";

    private static IResult RenderState(GameState state, Guid? viewer, string token, string joinUrl, Func<string, string?> resolveLogo, bool showJoinUrl = false) =>
        GameScreens.Select(state, viewer) switch
        {
            ScreenKind.LobbyHost => new RazorComponentResult<LobbyHostScreen>(new { Model = GameScreens.Lobby(state, viewer, token, joinUrl, showJoinUrl) }),
            ScreenKind.LobbyPlayer => new RazorComponentResult<LobbyPlayerScreen>(new { Model = GameScreens.Lobby(state, viewer, token, joinUrl, showJoinUrl: false) }),
            ScreenKind.Question => new RazorComponentResult<QuestionScreen>(new { Model = GameScreens.Question(state, token, QuestionStage.Direction, resolveLogo) }),
            ScreenKind.Waiting => new RazorComponentResult<WaitingScreen>(new { Model = GameScreens.Waiting(state, viewer) }),
            ScreenKind.DirectionResults => new RazorComponentResult<DirectionResultsScreen>(new { Model = GameScreens.DirectionResults(state, viewer, resolveLogo) }),
            ScreenKind.Results => new RazorComponentResult<ResultsScreen>(new { Model = GameScreens.Results(state, viewer, token, resolveLogo) }),
            ScreenKind.Standings => new RazorComponentResult<StandingsScreen>(new { Model = GameScreens.Standings(state, viewer) }),
            _ => throw new InvalidOperationException($"Unhandled screen kind for game {state.GameId}.")
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

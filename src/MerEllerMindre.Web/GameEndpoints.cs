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
    public static void MapGameEndpoints(this WebApplication app)
    {
        app.MapGet("/", (FileSystemQuestionPackCatalog catalog, IAntiforgery antiforgery, HttpContext http) =>
        {
            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            var packs = catalog.Packs.Select(p => new PackVm(p.PackId, p.Name, p.QuestionCount)).ToList();
            return new RazorComponentResult<QuizCatalog>(new { Model = new CatalogVm(packs, token) });
        });

        app.MapPost("/games", (
            [FromForm] string questionPackId,
            [FromForm] string hostName,
            GameApplicationService svc,
            PlayerIdentity identity,
            HttpContext http) =>
        {
            var result = svc.Open(new OpenLobby(hostName, questionPackId));
            if (result is not DomainOk ok || ok.Value is not [LobbyOpened opened, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

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
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");

            var result = svc.Execute(gameId, new JoinGame(state.JoinCode, playerName));
            if (result is not DomainOk ok || ok.Value is not [PlayerJoined joined, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

            identity.SetPlayer(http, gameId, joined.PlayerId);
            http.Response.Headers["HX-Redirect"] = $"/games/{state.JoinCode:N}";
            return Results.Ok();
        });

        app.MapGet("/games/{code}", (string code, GameApplicationService svc) =>
        {
            if (Resolve(svc, code) is not var (_, state))
                return Results.NotFound("Spelet hittades inte.");
            return new RazorComponentResult<GameShell>(new { JoinCode = state.JoinCode });
        });

        app.MapGet("/games/{code}/state", (string code, GameApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!);
        });

        app.MapPost("/games/{code}/start", (string code, GameApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            svc.Execute(gameId, new StartGame(gameId));
            return RenderState(svc.Load(gameId), identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!);
        });

        app.MapPost("/games/{code}/guess", (
            string code,
            [FromForm] string direction,
            [FromForm] string guessedDifference,
            GameApplicationService svc,
            PlayerIdentity identity,
            IAntiforgery antiforgery,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Spelet hittades inte.");

            var viewer = identity.GetPlayer(http, gameId);
            if (viewer is { } playerId
                && Enum.TryParse<Direction>(direction, out var dir)
                && decimal.TryParse(guessedDifference, NumberStyles.Number, CultureInfo.InvariantCulture, out var diff))
            {
                svc.Execute(gameId, new SubmitGuess(gameId, playerId, dir, diff));
                svc.RunScoreGear(gameId);
            }

            return RenderState(svc.Load(gameId), viewer, antiforgery.GetAndStoreTokens(http).RequestToken!);
        });

        app.MapPost("/games/{code}/next", (string code, GameApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Spelet hittades inte.");

            svc.RunProgressionGear(gameId);
            return RenderState(svc.Load(gameId), identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!);
        });
    }

    private static IResult RenderState(GameState state, Guid? viewer, string token) =>
        GameScreens.Select(state, viewer) switch
        {
            ScreenKind.LobbyHost => new RazorComponentResult<LobbyHostScreen>(new { Model = GameScreens.Lobby(state, viewer, token) }),
            ScreenKind.LobbyPlayer => new RazorComponentResult<LobbyPlayerScreen>(new { Model = GameScreens.Lobby(state, viewer, token) }),
            ScreenKind.Question => new RazorComponentResult<QuestionScreen>(new { Model = GameScreens.Question(state, token) }),
            ScreenKind.Waiting => new RazorComponentResult<WaitingScreen>(new { Model = GameScreens.Waiting(state, viewer) }),
            ScreenKind.Results => new RazorComponentResult<ResultsScreen>(new { Model = GameScreens.Results(state, viewer, token) }),
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
        AlreadyGuessed => "Du har redan gissat.",
        DifferenceOutOfRange => "Ogiltig gissning.",
        NotAllGuessesIn => "Alla har inte gissat än.",
        QuestionAlreadyScored => "Frågan är redan rättad."
    };
}

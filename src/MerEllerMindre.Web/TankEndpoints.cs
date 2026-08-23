using System.Text.Json;
using MerEllerMindre.Web.Components.TankTillTusen;
using MerEllerMindre.Web.Infrastructure;
using MerEllerMindre.Web.Presentation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TankTillTusen.Domain;
using TankOk = TankTillTusen.Domain.Ok<TankTillTusen.Domain.TankEvent[]>;

namespace MerEllerMindre.Web;

/// <summary>The DI services every Tank handler needs, bound as one <c>[AsParameters]</c> arg.</summary>
public record TankDeps(
    TankApplicationService Svc,
    PlayerIdentity Identity,
    IAntiforgery Antiforgery,
    Xm.XmCatalog Xm);

/// <summary>Everything a screen fragment needs to render, so RenderState takes one argument.</summary>
public record TankRenderContext(
    TankState State,
    Guid? Viewer,
    string Token,
    string JoinUrl,
    DateTimeOffset Now,
    bool ShowJoinUrl = false);

/// <summary>
/// Tänk Till Tusen (Countdown-style number game) HTTP endpoints, mounted under /tank-till-tusen —
/// parallel to MEM's /games and Blindbudet's /blindbudet, sharing the same static-SSR + htmx-poll
/// shell, PlayerIdentity cookie and antiforgery. Screens are RazorComponentResult fragments; the
/// create/join POSTs answer with HX-Redirect. No pack catalog — puzzles are generated per game.
/// </summary>
public static class TankEndpoints
{
    public static void MapTankEndpoints(this WebApplication app)
    {
        app.MapGet("/tank-till-tusen", GetCatalog);
        app.MapGet("/tank-till-tusen/om-spelet", () => new RazorComponentResult<TankOmSpelet>());
        app.MapGet("/tank-till-tusen/new", GetNew);
        app.MapPost("/tank-till-tusen", PostOpen);
        app.MapGet("/tank-till-tusen/{code}/join", GetJoin);
        app.MapPost("/tank-till-tusen/{code}/join", PostJoin);
        app.MapGet("/tank-till-tusen/{code}", GetGame);
        app.MapGet("/tank-till-tusen/{code}/state", GetState);
        app.MapPost("/tank-till-tusen/{code}/start", PostStart);
        app.MapPost("/tank-till-tusen/{code}/solution", PostSolution);
        app.MapPost("/tank-till-tusen/{code}/next", PostNext);
    }

    private static IResult GetCatalog() => new RazorComponentResult<TankCatalog>();

    private static IResult GetNew(string? difficulty, IAntiforgery antiforgery, HttpContext http)
    {
        var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
        return new RazorComponentResult<TankHostForm>(new { Model = new TankHostFormVm(token, ParseDifficulty(difficulty).ToString()) });
    }

    /// <summary>The host form's fields, bound as one <c>[FromForm]</c> arg. Init-properties, not
    /// constructor params: the form mapper treats every constructor parameter as required, and a
    /// hand-post without difficulty/roundCount should fall back to defaults rather than 400.</summary>
    public record OpenForm
    {
        public string HostName { get; init; } = "";
        public string? Difficulty { get; init; }
        public int? RoundCount { get; init; }
    }

    private static IResult PostOpen([FromForm] OpenForm form, [AsParameters] TankDeps d, HttpContext http)
    {
        var result = d.Svc.Open(new OpenLobby(form.HostName, ParseDifficulty(form.Difficulty), form.RoundCount ?? Decider.DefaultRoundCount));
        if (result is not TankOk ok || ok.Value is not [LobbyOpened opened, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Identity.SetPlayer(http, opened.GameId, opened.HostPlayerId);
        http.Response.Headers["HX-Redirect"] = $"/tank-till-tusen/{opened.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetJoin(string code, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Spelet hittades inte.");

        var token = d.Antiforgery.GetAndStoreTokens(http).RequestToken!;
        var hostName = state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        // xm defaults contract: JoinGame composes no view, so its form is transformer-defined.
        var spec = d.Xm.TankTillTusen;
        return new RazorComponentResult<Components.Xm.CommandDefaultPage>(new
        {
            Title = $"Tänk Till Tusen: {TankSurfaces.Label(spec, "JoinGame")}",
            LogoA = "Tänk Till ",
            LogoB = "Tusen",
            RolePill = TankSurfaces.Label(spec, "JoinGame"),
            Heading = "Gå med i spelet",
            Sub = $"{hostName} (värd)",
            PostPath = $"/tank-till-tusen/{state.JoinCode:N}/join",
            Token = token,
            InputName = "playerName",
            InputLabel = TankSurfaces.Label(spec, "JoinGame", "playerName"),
            SubmitLabel = TankSurfaces.Label(spec, "JoinGame"),
        });
    }

    private static IResult PostJoin(string code, [FromForm] string playerName, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Spelet hittades inte.");

        var result = d.Svc.Execute(gameId, new JoinGame(state.JoinCode, playerName));
        if (result is not TankOk ok || ok.Value is not [PlayerJoined joined, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Identity.SetPlayer(http, gameId, joined.PlayerId);
        http.Response.Headers["HX-Redirect"] = $"/tank-till-tusen/{state.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetGame(string code, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Spelet hittades inte.");
        return new RazorComponentResult<TankShell>(new { JoinCode = state.JoinCode, ShowJoinUrl = http.Request.Query.ContainsKey("url") });
    }

    private static IResult GetState(string code, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        // Any poll (waiting screen / a stuck puzzle screen) closes an expired round server-side.
        d.Svc.RunScoreGear(gameId);
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostStart(string code, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        d.Svc.Execute(gameId, new StartGame(gameId));
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostSolution(string code, [FromForm] string solution, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Spelet hittades inte.");

        var viewer = d.Identity.GetPlayer(http, gameId);
        if (viewer is { } playerId && Parse(solution) is { } parsed)
            d.Svc.Execute(gameId, new SubmitSolution(gameId, playerId, state.CurrentRoundIndex, parsed));

        // Whether the build was valid or the timer just expired: close the round if it's ready.
        d.Svc.RunScoreGear(gameId);
        return Rendered(d.Svc.Load(gameId), viewer, d, http);
    }

    private static IResult PostNext(string code, [AsParameters] TankDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Spelet hittades inte.");

        d.Svc.RunNextGear(gameId);
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    /// <summary>Trust boundary: an unknown/missing nivå falls back to Klassisk (the v1 behavior).</summary>
    private static Difficulty ParseDifficulty(string? value) =>
        Enum.TryParse<Difficulty>(value, ignoreCase: true, out var difficulty) ? difficulty : Difficulty.Klassisk;

    /// <summary>Parse the client's posted build (JSON: steps[] + answerIndex). Null = malformed.</summary>
    private static Solution? Parse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<SolutionDto>(json, JsonSerializerOptions.Web);
            if (dto?.Steps is null)
                return null; // malformed post — a real client post always has steps (possibly empty)
            var steps = dto.Steps.Select(s => new Step(s.LeftIndex, (Operator)s.Op, s.RightIndex)).ToList();
            return new Solution(steps, dto.AnswerIndex);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record SolutionDto(IReadOnlyList<StepDto>? Steps, int AnswerIndex);
    private sealed record StepDto(int LeftIndex, int Op, int RightIndex);

    private static string AbsoluteJoinUrl(HttpContext http, Guid joinCode) =>
        $"{http.Request.Scheme}://{http.Request.Host}/tank-till-tusen/{joinCode:N}/join";

    private static IResult Rendered(TankState state, Guid? viewer, TankDeps d, HttpContext http)
    {
        var c = new TankRenderContext(
            state, viewer,
            d.Antiforgery.GetAndStoreTokens(http).RequestToken!,
            AbsoluteJoinUrl(http, state.JoinCode),
            d.Svc.Now,
            http.Request.Query.ContainsKey("url"));
        // The Pussel surface opts out of the xm renderer: the räknartejp puzzle builder is a
        // hand-written idiom (xm finding 5). Every other screen is drawn by SurfaceRenderer.
        return TankSurfaces.Select(c.State, c.Viewer) == "Pussel"
            ? new RazorComponentResult<PuzzleScreen>(new { Model = TankSurfaces.Puzzle(c.State, c.Now, c.Token) })
            : new RazorComponentResult<Components.Xm.SurfaceRenderer>(new { Model = TankSurfaces.Screen(d.Xm.TankTillTusen, c) });
    }

    private static (Guid GameId, TankState State)? Resolve(TankApplicationService svc, string code)
    {
        if (!Guid.TryParse(code, out var joinCode))
            return null;
        var gameId = svc.ResolveJoinCode(joinCode);
        return gameId is null ? null : (gameId.Value, svc.Load(gameId.Value));
    }

    private static string Describe(TankError error) => error switch
    {
        GameNotFound => "Spelet hittades inte.",
        GameAlreadyStarted => "Spelet har redan startat.",
        NameAlreadyTaken => "Namnet är upptaget.",
        NotEnoughPlayers => "Det behövs minst 2 spelare.",
        AlreadySubmitted => "Du har redan låst ett svar på det här pusslet.",
        RoundAlreadyScored => "Pusslet är redan avslöjat.",
        DeadlinePassed => "Tiden är ute för det här pusslet.",
        InvalidSolution => "Ogiltigt svar.",
        NotReadyToScore => "Alla har inte svarat än.",
        RoundCountOutOfRange => "Välj mellan 4 och 21 pussel."
    };
}

using System.Globalization;
using Blindbudet.Domain;
using MerEllerMindre.Web.Components.Blindbudet;
using MerEllerMindre.Web.Infrastructure;
using MerEllerMindre.Web.Presentation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using AuctionOk = Blindbudet.Domain.Ok<Blindbudet.Domain.AuctionEvent[]>;

namespace MerEllerMindre.Web;

/// <summary>The DI services every auction handler needs, bound as one <c>[AsParameters]</c> arg.</summary>
public record AuctionDeps(
    AuctionApplicationService Svc,
    PlayerIdentity Identity,
    IAntiforgery Antiforgery);

/// <summary>Everything a screen fragment needs to render, so RenderState takes one argument.</summary>
public record AuctionRenderContext(
    AuctionState State,
    Guid? Viewer,
    string Token,
    string JoinUrl,
    bool ShowJoinUrl = false);

/// <summary>
/// Blindbudet (sealed-bid auction) HTTP endpoints, mounted under /blindbudet — parallel to
/// MEM's /games GameEndpoints, sharing the same static-SSR + htmx-polling shell, PlayerIdentity
/// cookie and antiforgery. Screens are RazorComponentResult fragments swapped into #screen; the
/// create/join POSTs answer with HX-Redirect to the auction shell.
/// </summary>
public static class AuctionEndpoints
{
    public static void MapAuctionEndpoints(this WebApplication app)
    {
        app.MapGet("/blindbudet", GetCatalog);
        app.MapGet("/blindbudet/new/{packId}", GetNew);
        app.MapPost("/blindbudet", PostOpen);
        app.MapGet("/blindbudet/{code}/join", GetJoin);
        app.MapPost("/blindbudet/{code}/join", PostJoin);
        app.MapGet("/blindbudet/{code}", GetGame);
        app.MapGet("/blindbudet/{code}/state", GetState);
        app.MapPost("/blindbudet/{code}/start", PostStart);
        app.MapPost("/blindbudet/{code}/bid", PostBid);
        app.MapPost("/blindbudet/{code}/next", PostNext);
    }

    private static IResult GetCatalog(FileSystemAuctionPackCatalog catalog)
    {
        var packs = catalog.Packs
            .Select(p => new AuctionPackVm(p.PackId, p.Name, p.LotCount))
            .ToList();
        return new RazorComponentResult<AuctionCatalog>(new { Model = new AuctionCatalogVm(packs) });
    }

    private static IResult GetNew(string packId, FileSystemAuctionPackCatalog catalog, IAntiforgery antiforgery, HttpContext http)
    {
        if (catalog.Find(packId) is not { } pack)
            return Results.NotFound("Lottpaketet hittades inte.");

        var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
        return new RazorComponentResult<AuctionHostForm>(new { Model = new AuctionHostFormVm(pack.PackId, pack.Name, token) });
    }

    private static IResult PostOpen([FromForm] string packId, [FromForm] string hostName, [AsParameters] AuctionDeps d, HttpContext http)
    {
        var result = d.Svc.Open(new OpenAuction(hostName, packId));
        if (result is not AuctionOk ok || ok.Value is not [AuctionOpened opened, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Identity.SetPlayer(http, opened.GameId, opened.HostPlayerId);
        http.Response.Headers["HX-Redirect"] = $"/blindbudet/{opened.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetJoin(string code, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Auktionen hittades inte.");

        var token = d.Antiforgery.GetAndStoreTokens(http).RequestToken!;
        var hostName = state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
        return new RazorComponentResult<AuctionJoinForm>(new { Model = new AuctionJoinFormVm(state.JoinCode, hostName, token) });
    }

    private static IResult PostJoin(string code, [FromForm] string playerName, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Auktionen hittades inte.");

        var result = d.Svc.Execute(gameId, new JoinAuction(state.JoinCode, playerName));
        if (result is not AuctionOk ok || ok.Value is not [PlayerJoined joined, ..])
            return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

        d.Identity.SetPlayer(http, gameId, joined.PlayerId);
        http.Response.Headers["HX-Redirect"] = $"/blindbudet/{state.JoinCode:N}";
        return Results.Ok();
    }

    private static IResult GetGame(string code, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (_, state))
            return Results.NotFound("Auktionen hittades inte.");
        return new RazorComponentResult<AuctionShell>(new { JoinCode = state.JoinCode, ShowJoinUrl = http.Request.Query.ContainsKey("url") });
    }

    private static IResult GetState(string code, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Auktionen hittades inte.");
        return Rendered(state, d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostStart(string code, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Auktionen hittades inte.");

        d.Svc.Execute(gameId, new StartAuction(gameId));
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static IResult PostBid(string code, [FromForm] string amount, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, state))
            return Results.NotFound("Auktionen hittades inte.");

        var viewer = d.Identity.GetPlayer(http, gameId);
        if (viewer is { } playerId && decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var bid))
        {
            d.Svc.Execute(gameId, new PlaceBid(gameId, playerId, state.CurrentLotIndex, bid));
            d.Svc.RunRevealGear(gameId);
        }
        return Rendered(d.Svc.Load(gameId), viewer, d, http);
    }

    private static IResult PostNext(string code, [AsParameters] AuctionDeps d, HttpContext http)
    {
        if (Resolve(d.Svc, code) is not var (gameId, _))
            return Results.NotFound("Auktionen hittades inte.");

        d.Svc.RunNextGear(gameId);
        return Rendered(d.Svc.Load(gameId), d.Identity.GetPlayer(http, gameId), d, http);
    }

    private static string AbsoluteJoinUrl(HttpContext http, Guid joinCode) =>
        $"{http.Request.Scheme}://{http.Request.Host}/blindbudet/{joinCode:N}/join";

    private static IResult Rendered(AuctionState state, Guid? viewer, AuctionDeps d, HttpContext http) =>
        RenderState(new AuctionRenderContext(
            state, viewer,
            d.Antiforgery.GetAndStoreTokens(http).RequestToken!,
            AbsoluteJoinUrl(http, state.JoinCode),
            http.Request.Query.ContainsKey("url")));

    private static IResult RenderState(AuctionRenderContext c) =>
        AuctionScreens.Select(c.State, c.Viewer) switch
        {
            AuctionScreenKind.LobbyHost => new RazorComponentResult<AuctionLobbyHostScreen>(new { Model = AuctionScreens.Lobby(c.State, c.Viewer, c.Token, c.JoinUrl, c.ShowJoinUrl) }),
            AuctionScreenKind.LobbyPlayer => new RazorComponentResult<AuctionLobbyPlayerScreen>(new { Model = AuctionScreens.Lobby(c.State, c.Viewer, c.Token, c.JoinUrl, showJoinUrl: false) }),
            AuctionScreenKind.Bid => new RazorComponentResult<AuctionBidScreen>(new { Model = AuctionScreens.Bid(c.State, c.Token) }),
            AuctionScreenKind.Waiting => new RazorComponentResult<AuctionWaitingScreen>(new { Model = AuctionScreens.Waiting(c.State, c.Viewer) }),
            AuctionScreenKind.RoundResults => new RazorComponentResult<AuctionRoundResultsScreen>(new { Model = AuctionScreens.RoundResults(c.State, c.Viewer, c.Token) }),
            AuctionScreenKind.Standings => new RazorComponentResult<AuctionStandingsScreen>(new { Model = AuctionScreens.Standings(c.State, c.Viewer) }),
            _ => throw new InvalidOperationException($"Unhandled auction screen kind for game {c.State.GameId}.")
        };

    private static (Guid GameId, AuctionState State)? Resolve(AuctionApplicationService svc, string code)
    {
        if (!Guid.TryParse(code, out var joinCode))
            return null;
        var gameId = svc.ResolveJoinCode(joinCode);
        return gameId is null ? null : (gameId.Value, svc.Load(gameId.Value));
    }

    private static string Describe(AuctionError error) => error switch
    {
        AuctionPackNotFound => "Lottpaketet hittades inte.",
        AuctionNotFound => "Auktionen hittades inte.",
        AuctionAlreadyStarted => "Auktionen har redan startat.",
        NameAlreadyTaken => "Namnet är upptaget.",
        NotEnoughPlayers => "Det behövs minst 2 spelare.",
        BidNegative => "Budet kan inte vara negativt.",
        AlreadyBid => "Du har redan lagt ett bud på den här lotten.",
        LotAlreadyResolved => "Lotten är redan avslöjad.",
        NotAllBidsIn => "Alla har inte lagt bud än."
    };
}

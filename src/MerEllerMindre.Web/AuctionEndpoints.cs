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
        app.MapGet("/blindbudet", (FileSystemAuctionPackCatalog catalog) =>
        {
            var packs = catalog.Packs
                .Select(p => new AuctionPackVm(p.PackId, p.Name, p.LotCount))
                .ToList();
            return new RazorComponentResult<AuctionCatalog>(new { Model = new AuctionCatalogVm(packs) });
        });

        app.MapGet("/blindbudet/new/{packId}", (string packId, FileSystemAuctionPackCatalog catalog, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (catalog.Find(packId) is not { } pack)
                return Results.NotFound("Lottpaketet hittades inte.");

            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            return new RazorComponentResult<AuctionHostForm>(new { Model = new AuctionHostFormVm(pack.PackId, pack.Name, token) });
        });

        app.MapPost("/blindbudet", (
            [FromForm] string packId,
            [FromForm] string hostName,
            AuctionApplicationService svc,
            PlayerIdentity identity,
            HttpContext http) =>
        {
            var result = svc.Open(new OpenAuction(hostName, packId));
            if (result is not AuctionOk ok || ok.Value is not [AuctionOpened opened, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

            identity.SetPlayer(http, opened.GameId, opened.HostPlayerId);
            http.Response.Headers["HX-Redirect"] = $"/blindbudet/{opened.JoinCode:N}";
            return Results.Ok();
        });

        app.MapGet("/blindbudet/{code}/join", (string code, AuctionApplicationService svc, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (_, state))
                return Results.NotFound("Auktionen hittades inte.");

            var token = antiforgery.GetAndStoreTokens(http).RequestToken!;
            var hostName = state.Players.FirstOrDefault(p => p.IsHost)?.Name ?? "";
            return new RazorComponentResult<AuctionJoinForm>(new { Model = new AuctionJoinFormVm(state.JoinCode, hostName, token) });
        });

        app.MapPost("/blindbudet/{code}/join", (
            string code,
            [FromForm] string playerName,
            AuctionApplicationService svc,
            PlayerIdentity identity,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Auktionen hittades inte.");

            var result = svc.Execute(gameId, new JoinAuction(state.JoinCode, playerName));
            if (result is not AuctionOk ok || ok.Value is not [PlayerJoined joined, ..])
                return Results.BadRequest(result is Err err ? Describe(err.Error) : "Något gick fel.");

            identity.SetPlayer(http, gameId, joined.PlayerId);
            http.Response.Headers["HX-Redirect"] = $"/blindbudet/{state.JoinCode:N}";
            return Results.Ok();
        });

        app.MapGet("/blindbudet/{code}", (string code, AuctionApplicationService svc, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (_, state))
                return Results.NotFound("Auktionen hittades inte.");
            return new RazorComponentResult<AuctionShell>(new { JoinCode = state.JoinCode, ShowJoinUrl = http.Request.Query.ContainsKey("url") });
        });

        app.MapGet("/blindbudet/{code}/state", (string code, AuctionApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Auktionen hittades inte.");
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode), http.Request.Query.ContainsKey("url"));
        });

        app.MapPost("/blindbudet/{code}/start", (string code, AuctionApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Auktionen hittades inte.");

            svc.Execute(gameId, new StartAuction(gameId));
            var state = svc.Load(gameId);
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode));
        });

        app.MapPost("/blindbudet/{code}/bid", (
            string code,
            [FromForm] string amount,
            AuctionApplicationService svc,
            PlayerIdentity identity,
            IAntiforgery antiforgery,
            HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, state))
                return Results.NotFound("Auktionen hittades inte.");

            var viewer = identity.GetPlayer(http, gameId);
            if (viewer is { } playerId && decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var bid))
            {
                svc.Execute(gameId, new PlaceBid(gameId, playerId, state.CurrentLotIndex, bid));
                svc.RunRevealGear(gameId);
            }

            var next = svc.Load(gameId);
            return RenderState(next, viewer, antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, next.JoinCode));
        });

        app.MapPost("/blindbudet/{code}/next", (string code, AuctionApplicationService svc, PlayerIdentity identity, IAntiforgery antiforgery, HttpContext http) =>
        {
            if (Resolve(svc, code) is not var (gameId, _))
                return Results.NotFound("Auktionen hittades inte.");

            svc.RunNextGear(gameId);
            var state = svc.Load(gameId);
            return RenderState(state, identity.GetPlayer(http, gameId), antiforgery.GetAndStoreTokens(http).RequestToken!, AbsoluteJoinUrl(http, state.JoinCode));
        });
    }

    private static string AbsoluteJoinUrl(HttpContext http, Guid joinCode) =>
        $"{http.Request.Scheme}://{http.Request.Host}/blindbudet/{joinCode:N}/join";

    private static IResult RenderState(AuctionState state, Guid? viewer, string token, string joinUrl, bool showJoinUrl = false) =>
        AuctionScreens.Select(state, viewer) switch
        {
            AuctionScreenKind.LobbyHost => new RazorComponentResult<AuctionLobbyHostScreen>(new { Model = AuctionScreens.Lobby(state, viewer, token, joinUrl, showJoinUrl) }),
            AuctionScreenKind.LobbyPlayer => new RazorComponentResult<AuctionLobbyPlayerScreen>(new { Model = AuctionScreens.Lobby(state, viewer, token, joinUrl, showJoinUrl: false) }),
            AuctionScreenKind.Bid => new RazorComponentResult<AuctionBidScreen>(new { Model = AuctionScreens.Bid(state, token) }),
            AuctionScreenKind.Waiting => new RazorComponentResult<AuctionWaitingScreen>(new { Model = AuctionScreens.Waiting(state, viewer) }),
            AuctionScreenKind.RoundResults => new RazorComponentResult<AuctionRoundResultsScreen>(new { Model = AuctionScreens.RoundResults(state, viewer, token) }),
            AuctionScreenKind.Standings => new RazorComponentResult<AuctionStandingsScreen>(new { Model = AuctionScreens.Standings(state, viewer) }),
            _ => throw new InvalidOperationException($"Unhandled auction screen kind for game {state.GameId}.")
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

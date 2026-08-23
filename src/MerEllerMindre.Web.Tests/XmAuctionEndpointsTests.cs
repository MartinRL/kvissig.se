using AwesomeAssertions;
using Blindbudet.Domain;
using MerEllerMindre.Web.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>TestAppFactory with the xm runtime renderer switched ON.</summary>
public sealed class XmTestAppFactory : TestAppFactory
{
    protected override bool XmBlindbudet => true;
}

/// <summary>
/// The D3 parity contract: the ENTIRE Blindbudet characterization suite re-run against the
/// xm runtime renderer (XmRenderer:Blindbudet = true). Same assertions, same Everest-8849
/// flow — if the interpreter drifts from the hand-written screens, this class goes red
/// while the base class stays green.
/// </summary>
public sealed class XmAuctionEndpointsTests : AuctionEndpointsTests, IClassFixture<XmTestAppFactory>
{
    public XmAuctionEndpointsTests(XmTestAppFactory factory) : base(factory) { }

    [Fact]
    public void EverySelectableSurfaceNameExistsInTheXmSpec()
    {
        var surfaces = Factory.Services.GetRequiredService<Web.Xm.XmCatalog>()
            .Blindbudet.Surfaces.Select(s => s.Name).ToHashSet();

        // The selector's complete co-domain (AuctionSurfaces.Select return literals).
        string[] reachable = ["LobbyVärd", "LobbySpelare", "Budgivning", "Väntan",
            "RundresultatVärd", "RundresultatSpelare", "Slutställning"];
        reachable.Should().OnlyContain(name => surfaces.Contains(name));
    }

    [Fact]
    public void SelectorMirrorsTheHandWrittenScreenSelection()
    {
        var lobby = new AuctionState { Phase = AuctionPhase.Lobby, HostPlayerId = Guid.NewGuid() };
        AuctionSurfaces.Select(lobby, lobby.HostPlayerId).Should().Be("LobbyVärd");
        AuctionSurfaces.Select(lobby, Guid.NewGuid()).Should().Be("LobbySpelare");
        AuctionSurfaces.Select(lobby with { Phase = AuctionPhase.Ended }, null).Should().Be("Slutställning");
    }
}

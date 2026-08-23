using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>
/// Blindbudet characterization suite over the real Web vertical (static-SSR fragments +
/// htmx form posts) — the Everest-8849 flow. One HttpClient per participant = one phone.
/// Assertions pin SEMANTIC markers (labels, CSS classes, ordering), not full HTML, so the
/// suite doubles as the parity oracle for the xm runtime renderer cut-over.
/// Deck: TestAppFactory's 2-lot "testauktion" pack — lot 0 Everest 8849 meter,
/// lot 1 equator 40075 km, played whole in file order (no mini sampling).
/// </summary>
public class AuctionEndpointsTests : IClassFixture<TestAppFactory>
{
    protected TestAppFactory Factory { get; }

    public AuctionEndpointsTests(TestAppFactory factory) => Factory = factory;

    private HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static readonly Regex TokenRx =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

    private static string Token(string html)
    {
        var m = TokenRx.Match(html);
        m.Success.Should().BeTrue("the rendered screen should carry an antiforgery token");
        return m.Groups[1].Value;
    }

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
    {
        var data = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token) };
        data.AddRange(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return new FormUrlEncodedContent(data);
    }

    private static async Task<string> OpenAuction(HttpClient host)
    {
        var token = Token(await (await host.GetAsync("/blindbudet/new/testauktion")).Content.ReadAsStringAsync());
        var resp = await host.PostAsync("/blindbudet", Form(token,
            ("packId", "testauktion"), ("hostName", "Martin")));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var redirect = resp.Headers.GetValues("HX-Redirect").Single();
        redirect.Should().StartWith("/blindbudet/");
        return redirect["/blindbudet/".Length..];
    }

    private static async Task Join(HttpClient player, string code, string name)
    {
        var joinHtml = await (await player.GetAsync($"/blindbudet/{code}/join")).Content.ReadAsStringAsync();
        var resp = await player.PostAsync($"/blindbudet/{code}/join", Form(Token(joinHtml), ("playerName", name)));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.GetValues("HX-Redirect").Single().Should().Be($"/blindbudet/{code}");
    }

    /// <summary>HtmlDecode makes the oracle entity-insensitive: hand-written markup carries
    /// emoji raw, @-expressions encode astral chars (&#x1F389;) — the DOM is identical.</summary>
    private static async Task<string> State(HttpClient client, string code) =>
        WebUtility.HtmlDecode(await (await client.GetAsync($"/blindbudet/{code}/state")).Content.ReadAsStringAsync());

    private static async Task Start(HttpClient host, string code) =>
        await host.PostAsync($"/blindbudet/{code}/start", Form(Token(await State(host, code))));

    /// <summary>Read the bid screen for its token, then place the hidden bid (invariant dot-decimal).</summary>
    private static async Task Bid(HttpClient client, string code, string amount) =>
        await client.PostAsync($"/blindbudet/{code}/bid",
            Form(Token(await State(client, code)), ("amount", amount)));

    private static async Task Next(HttpClient host, string code) =>
        await host.PostAsync($"/blindbudet/{code}/next", Form(Token(await State(host, code))));

    [Fact]
    public void XmCatalog_LoadsAndLintsTheBlindbudetSpecPairAtStartup()
    {
        var xm = Factory.Services.GetRequiredService<Web.Xm.XmCatalog>();

        xm.Blindbudet.Surfaces.Should().NotBeEmpty();
        xm.BlindbudetModel.PhaseValues.Should().Contain(["lobby", "started", "ended"]);
    }

    [Fact]
    public async Task Catalog_ListsTheAuctionPack()
    {
        var html = await (await NewClient().GetAsync("/blindbudet")).Content.ReadAsStringAsync();

        html.Should().Contain("Välj lek");
        html.Should().Contain("/blindbudet/new/testauktion");
    }

    [Fact]
    public async Task HostForm_ShowsANameInputForTheChosenPack()
    {
        var html = await (await NewClient().GetAsync("/blindbudet/new/testauktion")).Content.ReadAsStringAsync();

        html.Should().Contain("Skapa auktion");
        html.Should().Contain("name=\"hostName\"");
    }

    [Fact]
    public async Task HostForm_UnknownPackReturnsNotFound()
    {
        var resp = await NewClient().GetAsync("/blindbudet/new/bogus");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownAuction_StateReturnsNotFound()
    {
        var resp = await NewClient().GetAsync($"/blindbudet/{Guid.NewGuid():N}/state");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpenAuction_RedirectsHostToTheLobbyWithAScannableQr()
    {
        var host = NewClient();

        var code = await OpenAuction(host);

        var state = await (await host.GetAsync($"/blindbudet/{code}/state?url")).Content.ReadAsStringAsync();
        state.Should().Contain("Skanna för att gå med");
        state.Should().Contain("<svg");                        // a real, scannable QR is rendered
        state.Should().Contain($"/blindbudet/{code}/join");    // encoding the absolute join URL
        state.Should().Contain("Behöver minst 2 spelare");
    }

    [Fact]
    public async Task Join_ShowsThePlayerLobbyAndUnlocksStartForTheHost()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);

        await Join(player, code, "Nils");

        (await State(player, code)).Should().Contain("Du är med! 🎉");
        var hostLobby = await State(host, code);
        hostLobby.Should().Contain("Starta auktion");
        hostLobby.Should().Contain("Nils");
    }

    [Fact]
    public async Task Start_ShowsTheBidScreenWithKeypadAndHiddenAmount()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);
        await Join(player, code, "Nils");

        await Start(host, code);

        var bid = await State(player, code);
        bid.Should().Contain("Budrunda 1 / 2");
        bid.Should().Contain("Höjden på Mount Everest över havet");
        bid.Should().Contain("meter");                 // bid in the lot's own unit
        bid.Should().Contain("data-k=\"7\"");          // keypad keys
        bid.Should().Contain("name=\"amount\"");       // hidden invariant-decimal field
        bid.Should().Contain("Lägg bud!");
        bid.Should().NotContain("8849");               // true worth stays hidden while bidding
    }

    [Fact]
    public async Task OwnBid_ShowsTheWaitingScreenWithPendingPlayers()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        await Bid(host, code, "8000");

        var waiting = await State(host, code);
        waiting.Should().Contain("Ditt bud är lagt ✓");
        waiting.Should().Contain("1 av 2 klara");
        waiting.Should().Contain("Väntar på");
        waiting.Should().Contain("Nils");
    }

    [Fact]
    public async Task Reveal_ShowsTrueWorthWinnerAndTheOverbidCross()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        // Everest 8849: Martin 8000 (valid, wins), Nils 9000 (overbid → disqualified ✗).
        await Bid(host, code, "8000");
        await Bid(player, code, "9000");

        var results = await State(host, code);
        results.Should().Contain("Sant värde");
        results.Should().Contain("8849 meter");
        results.Should().Contain("Martin vann med budet 8000 meter.");
        results.Should().Contain("<span class=\"mark bad\">✗</span>");   // Nils's overbid
        results.Should().Contain("Nästa budrunda");                      // host advance button

        (await State(player, code)).Should().Contain("Väntar på att värden går vidare");
    }

    [Fact]
    public async Task Reveal_AllOverbid_NobodyWins()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        await Bid(host, code, "9000");
        await Bid(player, code, "10000");

        (await State(host, code)).Should().Contain("Ingen vann – alla bjöd över.");
    }

    [Fact]
    public async Task FullAuction_SharedWinThenDescendingStandingsCrownTheHighestTotal()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenAuction(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        // Lot 0 (Everest 8849): Martin 8000 → profit 10, Nils 9000 → overbid 0.
        await Bid(host, code, "8000");
        await Bid(player, code, "9000");
        await Next(host, code);

        // Lot 1 (equator 40075): both bid 30000 → shared win, profit 25 each.
        await Bid(host, code, "30000");
        await Bid(player, code, "30000");

        var round2 = await State(host, code);
        round2.Should().Contain("delade vinsten med budet 30000 km.");
        round2.Should().Contain("Visa slutställning");      // last lot: end button, not next

        await Next(host, code);

        var standings = await State(player, code);
        standings.Should().Contain("Slutställning");
        standings.Should().Contain("vann!");                // Martin 35 beats Nils 25 (HIGHEST wins)
        standings.Should().Contain("35");
        standings.Should().Contain("25");
        standings.IndexOf("Martin", StringComparison.Ordinal)
            .Should().BeLessThan(standings.IndexOf("Nils", StringComparison.Ordinal),
                "standings are ordered by total, descending");
        standings.Should().Contain("Spela igen");
    }
}

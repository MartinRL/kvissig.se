using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>
/// Tänk Till Tusen characterization suite over the real Web vertical (static-SSR fragments +
/// htmx form posts). One HttpClient per participant = one phone. Assertions pin SEMANTIC
/// markers (labels, CSS classes, ordering), not full HTML — the parity oracle for the xm
/// roll-over (ADR 019 Phase E).
/// Puzzles: TestAppFactory's stub generator — round 0 = [10,10]→100, round 1 = [5,20]→100
/// (0×1 = exact hit −10; 0+1 = a miss scored by raw distance). LOWEST total wins.
/// </summary>
public sealed class TankEndpointsTests : IClassFixture<TestAppFactory>
{
    private TestAppFactory Factory { get; }

    public TankEndpointsTests(TestAppFactory factory) => Factory = factory;

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

    // Solutions on the stub puzzles: 0×1 = exact hit (100), 0+1 = a miss (20 resp. 25).
    // Operator wire order: Add=0, Sub=1, Mul=2, Div=3.
    private const string Hit = """{"steps":[{"leftIndex":0,"op":2,"rightIndex":1}],"answerIndex":2}""";
    private const string Miss = """{"steps":[{"leftIndex":0,"op":0,"rightIndex":1}],"answerIndex":2}""";

    private static async Task<string> OpenGame(HttpClient host)
    {
        var token = Token(await (await host.GetAsync("/tank-till-tusen/new")).Content.ReadAsStringAsync());
        var resp = await host.PostAsync("/tank-till-tusen", Form(token,
            ("hostName", "Martin"), ("difficulty", "Klassisk"), ("roundCount", "4")));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var redirect = resp.Headers.GetValues("HX-Redirect").Single();
        redirect.Should().StartWith("/tank-till-tusen/");
        return redirect["/tank-till-tusen/".Length..];
    }

    private static async Task Join(HttpClient player, string code, string name)
    {
        var joinHtml = await (await player.GetAsync($"/tank-till-tusen/{code}/join")).Content.ReadAsStringAsync();
        var resp = await player.PostAsync($"/tank-till-tusen/{code}/join", Form(Token(joinHtml), ("playerName", name)));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.GetValues("HX-Redirect").Single().Should().Be($"/tank-till-tusen/{code}");
    }

    /// <summary>HtmlDecode makes the oracle entity-insensitive: hand-written markup carries
    /// emoji raw, @-expressions encode astral chars — the DOM is identical.</summary>
    private static async Task<string> State(HttpClient client, string code) =>
        WebUtility.HtmlDecode(await (await client.GetAsync($"/tank-till-tusen/{code}/state")).Content.ReadAsStringAsync());

    private static async Task Start(HttpClient host, string code) =>
        await host.PostAsync($"/tank-till-tusen/{code}/start", Form(Token(await State(host, code))));

    /// <summary>Read the current screen for its token, then lock a solution (JSON build).</summary>
    private static async Task Submit(HttpClient client, string code, string solution) =>
        await client.PostAsync($"/tank-till-tusen/{code}/solution",
            Form(Token(await State(client, code)), ("solution", solution)));

    private static async Task Next(HttpClient host, string code) =>
        await host.PostAsync($"/tank-till-tusen/{code}/next", Form(Token(await State(host, code))));

    [Fact]
    public void XmCatalog_LoadsAndLintsTheTankSpecPairAtStartup()
    {
        var xm = Factory.Services.GetRequiredService<Web.Xm.XmCatalog>();

        xm.TankTillTusen.Surfaces.Should().NotBeEmpty();
        xm.TankTillTusenModel.PhaseValues.Should().Contain(["lobby", "started", "ended"]);
    }

    [Fact]
    public async Task Catalog_ListsTheThreeDifficulties()
    {
        var html = await (await NewClient().GetAsync("/tank-till-tusen")).Content.ReadAsStringAsync();

        html.Should().Contain("Välj nivå");
        html.Should().Contain("/tank-till-tusen/new?difficulty=familj");
        html.Should().Contain("/tank-till-tusen/new?difficulty=klassisk");
        html.Should().Contain("/tank-till-tusen/new?difficulty=svår");
    }

    [Fact]
    public async Task HostForm_ShowsNameInputAndPuzzleCountSlider()
    {
        var html = await (await NewClient().GetAsync("/tank-till-tusen/new?difficulty=klassisk")).Content.ReadAsStringAsync();

        html.Should().Contain("Skapa spel");
        html.Should().Contain("name=\"hostName\"");
        html.Should().Contain("name=\"roundCount\"");
    }

    [Fact]
    public async Task UnknownGame_StateReturnsNotFound()
    {
        var resp = await NewClient().GetAsync($"/tank-till-tusen/{Guid.NewGuid():N}/state");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpenGame_RedirectsHostToTheLobbyWithAScannableQr()
    {
        var host = NewClient();

        var code = await OpenGame(host);

        var state = await (await host.GetAsync($"/tank-till-tusen/{code}/state?url")).Content.ReadAsStringAsync();
        state.Should().Contain("Skanna för att gå med");
        state.Should().Contain("<svg");                            // a real, scannable QR is rendered
        state.Should().Contain($"/tank-till-tusen/{code}/join");   // encoding the absolute join URL
        state.Should().Contain("Behöver minst 2 spelare");
    }

    [Fact]
    public async Task Join_ShowsThePlayerLobbyAndUnlocksStartForTheHost()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);

        await Join(player, code, "Nils");

        (await State(player, code)).Should().Contain("Du är med! 🎉");
        var hostLobby = await State(host, code);
        hostLobby.Should().Contain("Starta spel");
        hostLobby.Should().Contain("Nils");
    }

    [Fact]
    public async Task Start_ShowsThePuzzleWithNumbersTargetAndLockButton()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);
        await Join(player, code, "Nils");

        await Start(host, code);

        var puzzle = await State(player, code);
        puzzle.Should().Contain("Pussel 1 / 2");
        puzzle.Should().Contain("Nå så nära du kan");
        puzzle.Should().Contain("const START = [10,10]");   // the round-0 tal reach the tile script
        puzzle.Should().Contain("id=\"clock\"");            // cosmetic countdown
        puzzle.Should().Contain("name=\"solution\"");       // hidden JSON build field
        puzzle.Should().Contain("Lås svar!");
    }

    [Fact]
    public async Task OwnSolution_ShowsTheWaitingScreenWithPendingPlayers()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        await Submit(host, code, Miss);

        var waiting = await State(host, code);
        waiting.Should().Contain("Ditt svar är låst ✓");
        waiting.Should().Contain("1 av 2 klara");
        waiting.Should().Contain("Väntar på");
        waiting.Should().Contain("Nils");
        waiting.Should().Contain("räknar…");
    }

    [Fact]
    public async Task Reveal_ShowsTargetSampleSolutionAndDistanceScores()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        // Round 0 ([10,10]→100): Martin 10+10=20 (Δ80 → 80), Nils 10×10=100 (exact → −10).
        await Submit(host, code, Miss);
        await Submit(player, code, Hit);

        var results = await State(host, code);
        results.Should().Contain("Mål:");
        results.Should().Contain("Ett sätt:");
        results.Should().Contain("10 × 10 = 100");          // the sample tape, replayed readable
        results.Should().Contain("-10");                     // Nils's exact-hit bonus
        results.Should().Contain("80");                      // Martin's raw distance
        results.Should().Contain("Nästa pussel");            // host advance button

        (await State(player, code)).Should().Contain("Väntar på att värden går vidare");
    }

    [Fact]
    public async Task ExpiredRound_NonSubmitterGetsTheCrossAndWorstScore()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);
        await Join(player, code, "Nils");
        await Start(host, code);
        await Submit(host, code, Hit);

        try
        {
            // Push the stub clock past deadline + grace; the next poll's score gear closes the round.
            Factory.TankClockSkew = TimeSpan.FromSeconds(64);

            var results = await State(host, code);
            results.Should().Contain("Mål:");
            results.Should().Contain("<span class=\"mark bad\">✗</span>");   // Nils never locked
            results.Should().Contain("–");                                    // no reached value
            results.Should().Contain("100");                                  // worst score, flat
        }
        finally
        {
            Factory.TankClockSkew = TimeSpan.Zero;
        }
    }

    [Fact]
    public async Task FullGame_LowestTotalWinsWithAscendingStandings()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await OpenGame(host);
        await Join(player, code, "Nils");
        await Start(host, code);

        // Round 0: Martin 20 (Δ80 → 80), Nils 100 (exact → −10).
        await Submit(host, code, Miss);
        await Submit(player, code, Hit);
        await Next(host, code);

        // Round 1 ([5,20]→100): Martin 5×20=100 (−10 → 70), Nils 5+20=25 (Δ75 → 65).
        await Submit(host, code, Hit);
        await Submit(player, code, Miss);

        var round2 = await State(host, code);
        round2.Should().Contain("Pussel 2 / 2");
        round2.Should().Contain("Visa slutställning");      // last puzzle: end button, not next

        await Next(host, code);

        var standings = await State(player, code);
        standings.Should().Contain("Slutställning");
        standings.Should().Contain("vann!");                // Nils 65 beats Martin 70 (LOWEST wins)
        standings.Should().Contain("Lägst total poäng vinner.");
        standings.Should().Contain("65");
        standings.Should().Contain("70");
        standings.IndexOf("Nils", StringComparison.Ordinal)
            .Should().BeLessThan(standings.IndexOf("Martin", StringComparison.Ordinal),
                "standings are ordered by total, ascending");
        standings.Should().Contain("Spela igen");
    }
}

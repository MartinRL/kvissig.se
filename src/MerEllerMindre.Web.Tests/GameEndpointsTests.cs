using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>
/// End-to-end HTTP tests over the real Web vertical (static-SSR Razor fragments + htmx form
/// posts). Antiforgery is ON, so each POST first reads the token rendered into the preceding
/// screen (the per-client cookie jar carries the matching antiforgery + identity cookies).
/// One HttpClient per participant = one device.
/// </summary>
public class GameEndpointsTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public GameEndpointsTests(TestAppFactory factory) => _factory = factory;

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

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

    private static Task<string> CreateGame(HttpClient host) => CreateGame(host, "mer-eller-mindre");

    private static async Task<string> CreateGame(HttpClient host, string packId)
    {
        var token = Token(await (await host.GetAsync($"/games/new/{packId}")).Content.ReadAsStringAsync());
        var resp = await host.PostAsync("/games", Form(token,
            ("questionPackId", packId), ("hostName", "Martin")));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var redirect = resp.Headers.GetValues("HX-Redirect").Single();
        redirect.Should().StartWith("/games/");
        return redirect["/games/".Length..];
    }

    private static async Task Join(HttpClient player, string code, string name)
    {
        var joinHtml = await (await player.GetAsync($"/games/{code}/join")).Content.ReadAsStringAsync();
        var resp = await player.PostAsync($"/games/{code}/join", Form(Token(joinHtml), ("playerName", name)));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.GetValues("HX-Redirect").Single().Should().Be($"/games/{code}");
    }

    private static async Task<string> State(HttpClient client, string code) =>
        await (await client.GetAsync($"/games/{code}/state")).Content.ReadAsStringAsync();

    /// <summary>Stage 1: read the direction screen and lock in a mer/mindre call.</summary>
    private static async Task SubmitDirection(HttpClient client, string code, string direction) =>
        await client.PostAsync($"/games/{code}/direction",
            Form(Token(await State(client, code)), ("direction", direction)));

    /// <summary>Stage 2: tap through the mellansteg to the slider, then size the difference.</summary>
    private static async Task SubmitDifference(HttpClient client, string code, string value)
    {
        var slider = await (await client.GetAsync($"/games/{code}/difference")).Content.ReadAsStringAsync();
        await client.PostAsync($"/games/{code}/difference", Form(Token(slider), ("guessedDifference", value)));
    }

    [Fact]
    public async Task Catalog_ListsThePackFromTheCsvCatalog()
    {
        var html = await (await NewClient().GetAsync("/")).Content.ReadAsStringAsync();

        html.Should().Contain("Mer eller Mindre");
        html.Should().Contain("/games/new/mer-eller-mindre");

        // New pack gets a "Nytt" pill + is-new card, and bubbles right after the base deck.
        html.Should().Contain("<span class=\"pill new\">Nytt</span>");
        html.IndexOf("/games/new/mer-eller-mindre", StringComparison.Ordinal)
            .Should().BeLessThan(html.IndexOf("/games/new/loggor-mini-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OmSpelet_RendersContentAndSchema()
    {
        var html = await (await NewClient().GetAsync("/om-spelet")).Content.ReadAsStringAsync();

        html.Should().Contain("Vad är Mer eller Mindre?");
        html.Should().Contain("id=\"sa-spelar-du\"");
        html.Should().Contain("\"@type\":\"FAQPage\"");
        html.Should().Contain("rel=\"canonical\"");
        html.Should().Contain("og:title");
    }

    [Fact]
    public async Task SpelSom0100_RendersComparisonContentAndSchema()
    {
        var html = await (await NewClient().GetAsync("/spel-som-0-100")).Content.ReadAsStringAsync();

        html.Should().Contain("0-100");
        html.Should().Contain("\"@type\":\"FAQPage\"");
        html.Should().Contain("rel=\"canonical\"");
        html.Should().Contain("/spel-som-0-100");
    }

    [Fact]
    public async Task FragespelOnline_RendersContentAndSchema()
    {
        var html = await (await NewClient().GetAsync("/fragespel-online")).Content.ReadAsStringAsync();

        html.Should().Contain("\"@type\":\"FAQPage\"");
        html.Should().Contain("rel=\"canonical\"");
        html.Should().Contain("/fragespel-online");
    }

    [Fact]
    public async Task SpelSomMoreOrLess_RendersContentAndSchema()
    {
        var html = await (await NewClient().GetAsync("/spel-som-more-or-less")).Content.ReadAsStringAsync();

        html.Should().Contain("\"@type\":\"FAQPage\"");
        html.Should().Contain("rel=\"canonical\"");
        html.Should().Contain("/spel-som-more-or-less");
    }

    [Fact]
    public async Task UnknownUrl_Returns404WithCustomPage()
    {
        var resp = await NewClient().GetAsync("/finns-inte");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        (await resp.Content.ReadAsStringAsync()).Should().Contain("Sidan hittades inte");
    }

    [Fact]
    public async Task Sitemap_ListsCatalogAndContentPages()
    {
        var resp = await NewClient().GetAsync("/sitemap.xml");
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");

        var xml = await resp.Content.ReadAsStringAsync();
        xml.Should().Contain("<urlset");
        xml.Should().Contain("/om-spelet</loc>");
        xml.Should().Contain("/fragespel-online</loc>");
        xml.Should().Contain("/spel-som-more-or-less</loc>");
        xml.Should().Contain("/games/new/mer-eller-mindre</loc>");
    }

    [Fact]
    public async Task HostForm_ShowsANameInputForTheChosenPack()
    {
        var html = await (await NewClient().GetAsync("/games/new/mer-eller-mindre")).Content.ReadAsStringAsync();

        html.Should().Contain("Mer eller Mindre");
        html.Should().Contain("name=\"hostName\"");
    }

    [Fact]
    public async Task HostForm_UnknownPackReturnsNotFound()
    {
        var resp = await NewClient().GetAsync("/games/new/bogus");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownGame_StateReturnsNotFound()
    {
        var resp = await NewClient().GetAsync($"/games/{Guid.NewGuid():N}/state");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateGame_RedirectsHostToTheLobbyShell()
    {
        var host = NewClient();

        var code = await CreateGame(host);

        var state = await (await host.GetAsync($"/games/{code}/state?url")).Content.ReadAsStringAsync();
        state.Should().Contain("Skanna för att gå med");
        state.Should().Contain("<svg");                          // a real, scannable QR is rendered
        state.Should().Contain($"/games/{code}/join");           // encoding the absolute join URL
    }

    [Fact]
    public async Task HostSeesStartOnlyAfterASecondPlayerJoins()
    {
        var host = NewClient();
        var code = await CreateGame(host);

        (await State(host, code)).Should().Contain("Behöver minst 2 spelare");

        await Join(NewClient(), code, "Nils");

        var afterJoin = await State(host, code);
        afterJoin.Should().Contain("Starta omgång");
        afterJoin.Should().Contain("Nils");
    }

    [Fact]
    public async Task DirectionScreen_AsksWhichItemIsMerWithoutRevealingTheSlider()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");

        var lobby = await State(host, code);
        await host.PostAsync($"/games/{code}/start", Form(Token(lobby)));

        var question = await State(player, code);
        // Stage 1 is direction-only: the question + items show, but no slider/unit yet.
        question.Should().Contain("Har Danmark större eller mindre befolkning än Norge?");
        question.Should().Contain("Vilken är mer?");
        question.Should().NotContain("max=\"5.9\"");
    }

    [Fact]
    public async Task DifferenceScreen_ShowsAUnitSliderBoundedByTheLargerRawValueOnceDirectionRevealed()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        // Both call the direction → the reveal gear opens the mellansteg + stage 2.
        await SubmitDirection(host, code, "Mer");
        await SubmitDirection(player, code, "Mindre");

        var slider = await (await player.GetAsync($"/games/{code}/difference")).Content.ReadAsStringAsync();
        // Q0 = Danmark 5,9 / Norge 5,5 (miljoner invånare): slider 0 → max(A,B) in the card's unit.
        slider.Should().Contain("max=\"5.9\"");
        slider.Should().Contain("miljoner invånare");
    }

    [Fact]
    public async Task Mellansteg_RevealsTheDirectionAndDealsTheBonus()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        await SubmitDirection(host, code, "Mer");      // correct → −10
        await SubmitDirection(player, code, "Mindre");  // wrong → 0

        var mellansteg = await State(host, code);
        mellansteg.Should().Contain("mer eller mindre avslöjat");
        mellansteg.Should().Contain("Danmark");
        mellansteg.Should().Contain("MER");
        mellansteg.Should().Contain("-10");                 // Martin's bonus so far
        mellansteg.Should().Contain("Fortsätt till skillnaden");
    }

    [Fact]
    public async Task FullRound_AllGuessesInAutoScoresAndRevealsTheAnswer()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        // Stage 1: Martin calls Mer (correct), Nils calls Mindre (wrong) → reveal + bonus.
        await SubmitDirection(host, code, "Mer");
        await SubmitDirection(player, code, "Mindre");

        // Stage 2: both size 0,4. Martin (Mer, perfect) → roundScore -10; the last difference
        // fires the scoring gear.
        await SubmitDifference(host, code, "0.4");
        await SubmitDifference(player, code, "0.4");

        var results = await State(host, code);
        results.Should().Contain("Danmark");           // larger item revealed
        results.Should().Contain("MER");
        results.Should().Contain("% av mer");           // facit shown as slider-%
        results.Should().Contain("Mindre av mer");       // per-player answered-% column
        results.Should().Contain("Nästa fråga");        // host advance button
        results.Should().Contain("-10");                // Martin's round score
    }

    [Fact]
    public async Task HostAdvancesToTheNextQuestionViaTheProgressionGear()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        await SubmitDirection(host, code, "Mer");
        await SubmitDirection(player, code, "Mer");
        await SubmitDifference(host, code, "0.4");
        await SubmitDifference(player, code, "0.4");

        var results = await State(host, code);
        await host.PostAsync($"/games/{code}/next", Form(Token(results)));

        var q1 = await State(player, code);
        // Q1 = Sverige 450295 / Norge 385207 (km²) — stage-1 direction screen.
        q1.Should().Contain("Är Sveriges yta större eller mindre än Norges?");
    }

    [Fact]
    public async Task LogoMode_HidesNamesOnTheQuestionScreenAndRevealsThemOnResults()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host, "loggor-mini-1");
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        // Q0 = Volvo / Ericsson: the direction screen shows logos and hides the names.
        var question = await State(player, code);
        question.Should().Contain("class=\"logoimg\"");
        question.Should().Contain("/logos/se/volvo.png");
        question.Should().NotContain("Volvo");
        question.Should().NotContain("Ericsson");

        // Play the round through to the reveal.
        await SubmitDirection(host, code, "Mer");
        await SubmitDirection(player, code, "Mindre");
        await SubmitDifference(host, code, "210");
        await SubmitDifference(player, code, "210");

        var results = await State(host, code);
        results.Should().Contain("Volvo");      // names revealed on the results screen
        results.Should().Contain("Ericsson");
    }

    [Fact]
    public async Task LogoMode_DifferenceScreenHasAPercentElement()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host, "loggor-mini-1");
        await Join(player, code, "Nils");
        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));
        await SubmitDirection(host, code, "Mer");
        await SubmitDirection(player, code, "Mindre");

        // The %-label lives in its own server-rendered element so logo mode can update it
        // without wiping the <img> (regression guard for the missing % on logo packs).
        var diff = await (await host.GetAsync($"/games/{code}/difference")).Content.ReadAsStringAsync();
        diff.Should().Contain("class=\"barpct\"");
    }
}

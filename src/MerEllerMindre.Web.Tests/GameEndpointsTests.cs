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
public class GameEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

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

    private static async Task<string> CreateGame(HttpClient host)
    {
        var token = Token(await (await host.GetAsync("/games/new/mer-eller-mindre")).Content.ReadAsStringAsync());
        var resp = await host.PostAsync("/games", Form(token,
            ("questionPackId", "mer-eller-mindre"), ("hostName", "Martin")));
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

    [Fact]
    public async Task Catalog_ListsThePackFromTheCsvCatalog()
    {
        var html = await (await NewClient().GetAsync("/")).Content.ReadAsStringAsync();

        html.Should().Contain("Mer eller mindre");
        html.Should().Contain("/games/new/mer-eller-mindre");
    }

    [Fact]
    public async Task HostForm_ShowsANameInputForTheChosenPack()
    {
        var html = await (await NewClient().GetAsync("/games/new/mer-eller-mindre")).Content.ReadAsStringAsync();

        html.Should().Contain("Mer eller mindre");
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

        var state = await State(host, code);
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
    public async Task QuestionScreen_ShowsAUnitSliderBoundedByTheLargerRawValue()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");

        var lobby = await State(host, code);
        await host.PostAsync($"/games/{code}/start", Form(Token(lobby)));

        var question = await State(player, code);
        // Q0 = Danmark 5,9 / Norge 5,5 (miljoner invånare): slider 0 → max(A,B) in the card's unit.
        question.Should().Contain("max=\"5.9\"");
        question.Should().Contain("miljoner invånare");
        question.Should().Contain("Har Danmark större eller mindre befolkning än Norge?");
    }

    [Fact]
    public async Task FullRound_AllGuessesInAutoScoresAndRevealsTheAnswer()
    {
        var host = NewClient();
        var player = NewClient();
        var code = await CreateGame(host);
        await Join(player, code, "Nils");

        await host.PostAsync($"/games/{code}/start", Form(Token(await State(host, code))));

        // Martin: Mer 0,4 (correct direction, perfect diff) → roundScore -10.
        var q1 = await State(host, code);
        var afterHostGuess = await host.PostAsync($"/games/{code}/guess",
            Form(Token(q1), ("direction", "Mer"), ("guessedDifference", "0.4")));
        (await afterHostGuess.Content.ReadAsStringAsync()).Should().Contain("Din gissning är ställd");

        // Nils: Mindre 0,4 (wrong direction) → roundScore +7. Last guess fires the scoring gear.
        var q2 = await State(player, code);
        await player.PostAsync($"/games/{code}/guess",
            Form(Token(q2), ("direction", "Mindre"), ("guessedDifference", "0.4")));

        var results = await State(host, code);
        results.Should().Contain("Danmark");           // larger item revealed
        results.Should().Contain("MER");
        results.Should().Contain("/ 100");              // normalized facit shown
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

        await host.PostAsync($"/games/{code}/guess",
            Form(Token(await State(host, code)), ("direction", "Mer"), ("guessedDifference", "0.4")));
        await player.PostAsync($"/games/{code}/guess",
            Form(Token(await State(player, code)), ("direction", "Mer"), ("guessedDifference", "0.4")));

        var results = await State(host, code);
        await host.PostAsync($"/games/{code}/next", Form(Token(results)));

        var q1 = await State(player, code);
        // Q1 = Sverige 450295 / Norge 385207 (km²).
        q1.Should().Contain("Är Sveriges yta större eller mindre än Norges?");
        q1.Should().Contain("km²");
    }
}

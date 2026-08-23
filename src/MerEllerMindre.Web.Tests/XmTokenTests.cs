using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MerEllerMindre.Web.Xm;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>Plan D2 token contract: NO token generation (YAGNI) — instead assert every xm
/// design-token value already lives in playful.css, so the spec and the stylesheet cannot
/// silently drift apart.</summary>
public sealed class XmTokenTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public XmTokenTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task EveryXmTokenValueExistsInPlayfulCss()
    {
        var css = await _factory.CreateClient().GetStringAsync("/css/playful.css");
        var tokens = _factory.Services.GetRequiredService<XmCatalog>().Blindbudet.Tokens;

        tokens.Should().NotBeEmpty();
        foreach (var token in tokens)
        {
            // fontFamily values are joined lists ("Baloo 2, system-ui, ..."); the css quotes
            // the family name, so assert the first segment only. Colors/dimensions are exact.
            var needle = token.Value.Split(',')[0].Trim();
            css.Should().Contain(needle, $"xm token {token.Path} must exist in playful.css");
        }
    }
}

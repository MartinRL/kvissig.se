using AwesomeAssertions;
using MerEllerMindre.Web.Components.Xm;
using MerEllerMindre.Web.Xm;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xmlang;
using Xunit;

namespace MerEllerMindre.Web.Tests;

/// <summary>xmlang v0.3.0-experimental `slot:` (geometry-prohibition experiment, see
/// articles/xmlang-geometry-experiment.md): parse the key, lint unknown values as
/// `xm-unknown-slot` errors and default-restating command slots as
/// `xm-slot-restates-default` info. Lint helpers take xm-side types ONLY (tripwire e).</summary>
public sealed class XmSlotTests
{
    private const string Em = """
        slices:
          Demo:
            - c: DoThing
            - v: State / Things
              props:
                stuff: string
        """;

    private static XmSpec Xm(string viewSlot, string commandSlot) => XmParser.Parse($"""
        xmlang: "0.3"
        surfaces:
          Demo:
            compose:
              - v: State / Things
                {viewSlot}
              - c: DoThing
                {commandSlot}
        """);

    [Fact]
    public void SlotIsParsedPerComposeItem()
    {
        var xm = Xm("slot: header", "slot: body");

        xm.Surfaces[0].Compose[0].Slot.Should().Be("header");
        xm.Surfaces[0].Compose[1].Slot.Should().Be("body");
    }

    [Fact]
    public void AbsentSlotParsesAsNull()
    {
        var xm = XmParser.Parse("""
            xmlang: "0.3"
            surfaces:
              Demo:
                compose:
                  - v: State / Things
                  - c: DoThing
            """);

        xm.Surfaces[0].Compose[0].Slot.Should().BeNull();
        xm.Surfaces[0].Compose[1].Slot.Should().BeNull();
    }

    [Fact]
    public void UnknownSlotValueIsAnError()
    {
        var findings = XmLinter.Lint(Xm("slot: sidebar", "slot: footer"), EmModel.Parse(Em));

        findings.Should().Contain(f =>
            f.Rule == "xm-unknown-slot" && f.Severity == XmSeverity.Error && f.Message.Contains("sidebar"));
    }

    [Fact]
    public void CommandSlotFooterRestatesTheDefault()
    {
        var findings = XmLinter.Lint(Xm("slot: header", "slot: footer"), EmModel.Parse(Em));

        findings.Should().Contain(f =>
            f.Rule == "xm-slot-restates-default" && f.Severity == XmSeverity.Info && f.Message.Contains("DoThing"));
    }

    [Fact]
    public void GenuineSlotsLintClean()
    {
        // View slots have no model default (transformer judgment) and a header command
        // is a genuine judgment — neither may produce a slot finding.
        var findings = XmLinter.Lint(Xm("slot: footer", "slot: header"), EmModel.Parse(Em));

        findings.Should().NotContain(f => f.Rule == "xm-unknown-slot" || f.Rule == "xm-slot-restates-default");
    }

    // ---- SurfaceRenderer region rendering (Phase 3) ----

    private static async Task<string> Render(XmScreenModel model)
    {
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<SurfaceRenderer>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Model"] = model }));
            return output.ToHtmlString();
        });
    }

    [Fact]
    public async Task SlottedItemsRenderInTheirRegions()
    {
        var surface = new XmSurface("Demo", [], [],
        [
            new(new XmViewItem("State / A", ["alpha"], [], [], null), null),
            new(new XmViewItem("State / B", ["beta"], [], [], null), null, "header"),
            new(null, new XmCommandItem("DoThing", "primary"), "header"),
            new(null, new XmCommandItem("Other", "primary")),
            new(new XmViewItem("State / C", ["gamma"], [], [], null), null, "footer"),
        ]);
        var model = new XmScreenModel(
            surface,
            new Dictionary<string, Field>
            {
                ["alpha"] = new TextField("ALPHA"),
                ["beta"] = new TextField("BETA"),
                ["gamma"] = new TextField("GAMMA"),
            },
            [new CommandModel("DoThing", "Gör", "/x/do"), new CommandModel("Other", "Annat", "/x/other")],
            "tok",
            Heading: "Rubrik",
            Footer: "FOOT");

        var html = await Render(model);

        // header region inside the header card, after default header content; body default
        // command position; footer region after commands, before footer chrome.
        var order = new[] { "Rubrik", "ALPHA", "BETA", "/x/do", "/x/other", "GAMMA", "FOOT" };
        var indexes = order.Select(needle => html.IndexOf(needle, StringComparison.Ordinal)).ToArray();
        indexes.Should().OnlyContain(i => i >= 0);
        indexes.Should().BeInAscendingOrder("slotted items must land in their regions: {0}", html);
    }

    [Fact]
    public async Task NoSlotSurfaceRendersExactlyAsBeforeSlotExisted()
    {
        var surface = new XmSurface("Demo", [], [],
        [
            new(new XmViewItem("State / A", ["alpha"], [], ["delta"], null), null),
            new(null, new XmCommandItem("DoThing", "primary")),
        ]);
        var model = new XmScreenModel(
            surface,
            new Dictionary<string, Field> { ["alpha"] = new TextField("ALPHA"), ["delta"] = new TextField("DELTA") },
            [new CommandModel("DoThing", "Gör", "/x/do")],
            "tok",
            Heading: "Rubrik");

        var html = await Render(model);

        // Pinned v0.2 layout (inter-tag whitespace collapsed): any drift in the
        // no-slot path is a parity break.
        System.Text.RegularExpressions.Regex.Replace(html, @">\s+<", "><").Trim().Should().Be(
            "<div><div class=\"card center\"><h1>Rubrik</h1><p class=\"sub\">ALPHA</p></div>"
            + "<details class=\"ondemand\"><summary>Mer</summary><p class=\"sub\">DELTA</p></details>"
            + "<form hx-post=\"/x/do\" hx-target=\"#screen\" hx-swap=\"innerHTML\">"
            + "<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"tok\" />"
            + "<button type=\"submit\" class=\"btn\">G&#xF6;r</button></form></div>");
    }
}

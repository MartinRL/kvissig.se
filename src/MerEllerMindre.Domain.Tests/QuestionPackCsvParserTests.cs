using AwesomeAssertions;
using Xunit;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Tests for the pure CSV parser (no IO) plus one test that loads the real
/// data/packs/mer-eller-mindre.csv from disk and parses it.
/// </summary>
public class QuestionPackCsvParserTests
{
    private const string Header = "fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga";

    [Fact]
    public void ParsesValidSwedishExcelCsv()
    {
        var csv = Header + "\n" +
                  "Har Danmark större eller mindre befolkning än Norge?;Danmark;Norge;5,9;5,5;miljoner invånare;Hur många miljoner invånare skiljer det?\n";

        var pack = QuestionPackCsvParser.Parse("mer-eller-mindre", csv);

        pack.PackId.Should().Be("mer-eller-mindre");
        pack.Name.Should().Be("Mer eller Mindre");
        pack.QuestionCount.Should().Be(1);

        var q = pack.Questions[0];
        q.QuestionText.Should().Be("Har Danmark större eller mindre befolkning än Norge?");
        q.ItemA.Should().Be("Danmark");
        q.ItemB.Should().Be("Norge");
        q.ValueA.Should().Be(5.9m);
        q.ValueB.Should().Be(5.5m);
        q.Unit.Should().Be("miljoner invånare");
        q.DifferencePrompt.Should().Be("Hur många miljoner invånare skiljer det?");
    }

    [Fact]
    public void ParsesCommaDecimalsViaSvSe()
    {
        var csv = Header + "\n" + "fråga;A;B;1234,5;0,25;st;Hur många st skiljer det?\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.ValueA.Should().Be(1234.5m);
        q.ValueB.Should().Be(0.25m);
    }

    [Fact]
    public void HonoursQuotedFieldWithSeparatorAndNewline()
    {
        var csv = Header + "\n" +
                  "\"Är A större, eller mindre;\nverkligen?\";A;B;10;5;kg;Hur många kg skiljer det?\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.QuestionText.Should().Be("Är A större, eller mindre;\nverkligen?");
        q.ItemA.Should().Be("A");
        q.ValueA.Should().Be(10m);
    }

    [Fact]
    public void StripsBomAndSkipsBlankRows()
    {
        var csv = "\uFEFF" + Header + "\n" +
                  "\n" +
                  "fråga;A;B;3;2;m;Hur många m skiljer det?\n" +
                  "   \n";

        var pack = QuestionPackCsvParser.Parse("p", csv);

        pack.QuestionCount.Should().Be(1);
        pack.Questions[0].ValueA.Should().Be(3m);
    }

    [Fact]
    public void MapsColumnsByHeaderNameRegardlessOfOrder()
    {
        var csv = "enhet;värdeB;värdeA;sakB;sakA;fråga;differensfråga\n" +
                  "kg;5;10;B;A;Väger A mer än B?;Hur många kg skiljer det?\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.QuestionText.Should().Be("Väger A mer än B?");
        q.ItemA.Should().Be("A");
        q.ItemB.Should().Be("B");
        q.ValueA.Should().Be(10m);
        q.ValueB.Should().Be(5m);
        q.Unit.Should().Be("kg");
        q.DifferencePrompt.Should().Be("Hur många kg skiljer det?");
    }

    [Fact]
    public void ThrowsOnMissingColumn()
    {
        var csv = "fråga;sakA;sakB;värdeA;värdeB;differensfråga\n" +
                  "Q;A;B;10;5;Hur många skiljer det?\n";

        var act = () => QuestionPackCsvParser.Parse("p", csv);

        act.Should().Throw<FormatException>().WithMessage("*enhet*");
    }

    [Fact]
    public void ThrowsOnMissingDifferencePromptColumn()
    {
        var csv = "fråga;sakA;sakB;värdeA;värdeB;enhet\n" +
                  "Q;A;B;10;5;kg\n";

        var act = () => QuestionPackCsvParser.Parse("p", csv);

        act.Should().Throw<FormatException>().WithMessage("*differensfråga*");
    }

    [Fact]
    public void ThrowsOnUnparseableNumber()
    {
        var csv = Header + "\n" + "Q;A;B;tio;5;kg;Hur många kg skiljer det?\n";

        var act = () => QuestionPackCsvParser.Parse("p", csv);

        act.Should().Throw<FormatException>().WithMessage("*värdeA*");
    }

    [Fact]
    public void ThrowsOnEmptyFile()
    {
        var act = () => QuestionPackCsvParser.Parse("p", "");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ThrowsOnHeaderOnly()
    {
        var act = () => QuestionPackCsvParser.Parse("p", Header + "\n");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void AppliesDisplayNameOverrideForAllaAldrar()
    {
        // Plain de-slug would drop the å ("Alla aldrar"); the override restores it.
        var csv = Header + "\n" + "Q;A;B;10;5;kg;Hur många kg skiljer det?\n";

        var pack = QuestionPackCsvParser.Parse("alla-aldrar", csv);

        pack.Name.Should().Be("Mer eller Mindre – alla åldrar");
    }

    // The pack CSVs are copied to output beside the Domain assembly (see its csproj).
    private static readonly string PacksDir = Path.Combine(AppContext.BaseDirectory, "data", "packs");

    public static TheoryData<string> AllPacks()
    {
        var data = new TheoryData<string>();
        foreach (var f in Directory.GetFiles(PacksDir, "*.csv").Where(f => !f.EndsWith(".källor.csv")))
            data.Add(f);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllPacks))]
    public void EveryFullDeckIsExactly1085Cards(string path)
    {
        var slug = Path.GetFileNameWithoutExtension(path);

        // "mini" packs are concept decks (deliberately short, e.g. 175 cards / 7-question round)
        // tested before scaling to a full prod deck — they are exempt from the 1085 contract.
        if (slug.Contains("mini"))
            return;

        var pack = QuestionPackCsvParser.Parse(slug, File.ReadAllText(path));

        pack.QuestionCount.Should().Be(1085);
    }

    [Theory]
    [MemberData(nameof(AllPacks))]
    public void EveryPackHasCleanCards(string path)
    {
        var slug = Path.GetFileNameWithoutExtension(path);
        var pack = QuestionPackCsvParser.Parse(slug, File.ReadAllText(path));

        pack.Questions.Should().OnlyContain(q =>
            q.QuestionText.Length > 0 && q.ItemA.Length > 0 && q.ItemB.Length > 0
            && q.Unit.Length > 0 && q.DifferencePrompt.Length > 0);

        // Part-vs-whole / same-entity tautologies have no real guess — never ship them.
        var smells = pack.Questions
            .Select(q => (q, smell: QuestionChecks.SameEntitySmell(q)))
            .Where(t => t.smell.flagged)
            .Select(t => $"{t.q.QuestionText} ({t.smell.reason})")
            .ToList();
        smells.Should().BeEmpty();
    }

    [Fact]
    public void MerEllerMindreFirstCardIsEverestVsK2()
    {
        var path = Path.Combine(PacksDir, "mer-eller-mindre.csv");
        var pack = QuestionPackCsvParser.Parse("mer-eller-mindre", File.ReadAllText(path));

        pack.Name.Should().Be("Mer eller Mindre");

        // First card: Mount Everest vs K2. Convention: Mer = sakA holds the larger value.
        var first = pack.Questions[0];
        first.QuestionText.Should().Be("Är Mount Everest högre eller lägre än K2?");
        first.ItemA.Should().Be("Mount Everest");
        first.ItemB.Should().Be("K2");
        first.ValueA.Should().Be(8849m);
        first.ValueB.Should().Be(8611m);
        first.Unit.Should().Be("meter");
        first.DifferencePrompt.Should().Be("Hur många meter skiljer det?");
        first.ValueA.Should().BeGreaterThan(first.ValueB, "Mer = sakA holds the larger value");
    }

    [Fact]
    public void AllaAldrarPackHasDisplayName()
    {
        var path = Path.Combine(PacksDir, "alla-aldrar.csv");
        var pack = QuestionPackCsvParser.Parse("alla-aldrar", File.ReadAllText(path));

        pack.Name.Should().Be("Mer eller Mindre – alla åldrar");
    }

    [Fact]
    public void SameEntitySmellFlagsPartVsWholeButNotLegitPairs()
    {
        var brain = new Question("Väger vänstra hjärnhalvan mer eller mindre än hela hjärnan?",
            "Vänstra hjärnhalvan", "Hela hjärnan", 680, 1300, "gram", "Hur många gram skiljer det?");
        var everest = new Question("Är Mount Everest högre eller lägre än K2?",
            "Mount Everest", "K2", 8849, 8611, "meter", "Hur många meter skiljer det?");
        var slott = new Question("Är Stockholms slott större eller mindre än Drottningholms slott?",
            "Stockholms slott", "Drottningholms slott", 2, 1, "hektar", "Hur många hektar skiljer det?");

        QuestionChecks.SameEntitySmell(brain).flagged.Should().BeTrue();
        QuestionChecks.SameEntitySmell(everest).flagged.Should().BeFalse();
        QuestionChecks.SameEntitySmell(slott).flagged.Should().BeFalse();
    }
}

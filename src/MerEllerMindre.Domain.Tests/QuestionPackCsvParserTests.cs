using FluentAssertions;
using Xunit;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Tests for the pure CSV parser (no IO) plus one test that loads the real
/// data/packs/mer-eller-mindre.csv from disk and parses it.
/// </summary>
public class QuestionPackCsvParserTests
{
    private const string Header = "fråga;sakA;sakB;värdeA;värdeB;enhet";

    [Fact]
    public void ParsesValidSwedishExcelCsv()
    {
        var csv = Header + "\n" +
                  "Har Danmark större eller mindre befolkning än Norge?;Danmark;Norge;5,9;5,5;miljoner invånare\n";

        var pack = QuestionPackCsvParser.Parse("mer-eller-mindre", csv);

        pack.PackId.Should().Be("mer-eller-mindre");
        pack.Name.Should().Be("Mer eller mindre");
        pack.QuestionCount.Should().Be(1);

        var q = pack.Questions[0];
        q.QuestionText.Should().Be("Har Danmark större eller mindre befolkning än Norge?");
        q.ItemA.Should().Be("Danmark");
        q.ItemB.Should().Be("Norge");
        q.ValueA.Should().Be(5.9);
        q.ValueB.Should().Be(5.5);
        q.Unit.Should().Be("miljoner invånare");
    }

    [Fact]
    public void ParsesCommaDecimalsViaSvSe()
    {
        var csv = Header + "\n" + "fråga;A;B;1234,5;0,25;st\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.ValueA.Should().Be(1234.5);
        q.ValueB.Should().Be(0.25);
    }

    [Fact]
    public void HonoursQuotedFieldWithSeparatorAndNewline()
    {
        var csv = Header + "\n" +
                  "\"Är A större, eller mindre;\nverkligen?\";A;B;10;5;kg\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.QuestionText.Should().Be("Är A större, eller mindre;\nverkligen?");
        q.ItemA.Should().Be("A");
        q.ValueA.Should().Be(10);
    }

    [Fact]
    public void StripsBomAndSkipsBlankRows()
    {
        var csv = "\uFEFF" + Header + "\n" +
                  "\n" +
                  "fråga;A;B;3;2;m\n" +
                  "   \n";

        var pack = QuestionPackCsvParser.Parse("p", csv);

        pack.QuestionCount.Should().Be(1);
        pack.Questions[0].ValueA.Should().Be(3);
    }

    [Fact]
    public void MapsColumnsByHeaderNameRegardlessOfOrder()
    {
        var csv = "enhet;värdeB;värdeA;sakB;sakA;fråga\n" +
                  "kg;5;10;B;A;Väger A mer än B?\n";

        var q = QuestionPackCsvParser.Parse("p", csv).Questions[0];

        q.QuestionText.Should().Be("Väger A mer än B?");
        q.ItemA.Should().Be("A");
        q.ItemB.Should().Be("B");
        q.ValueA.Should().Be(10);
        q.ValueB.Should().Be(5);
        q.Unit.Should().Be("kg");
    }

    [Fact]
    public void ThrowsOnMissingColumn()
    {
        var csv = "fråga;sakA;sakB;värdeA;värdeB\n" +
                  "Q;A;B;10;5\n";

        var act = () => QuestionPackCsvParser.Parse("p", csv);

        act.Should().Throw<FormatException>().WithMessage("*enhet*");
    }

    [Fact]
    public void ThrowsOnUnparseableNumber()
    {
        var csv = Header + "\n" + "Q;A;B;tio;5;kg\n";

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
    public void LoadsTheRealMerEllerMindrePack()
    {
        // The pack CSVs are copied to output beside the Domain assembly (see its csproj).
        var path = Path.Combine(AppContext.BaseDirectory, "data", "packs", "mer-eller-mindre.csv");
        var text = File.ReadAllText(path);

        var pack = QuestionPackCsvParser.Parse("mer-eller-mindre", text);

        pack.Name.Should().Be("Mer eller mindre");
        pack.QuestionCount.Should().Be(10);

        // First card: Danmark vs Norge. Convention: Mer = sakA holds the larger value.
        var first = pack.Questions[0];
        first.QuestionText.Should().Be("Har Danmark större eller mindre befolkning än Norge?");
        first.ItemA.Should().Be("Danmark");
        first.ItemB.Should().Be("Norge");
        first.ValueA.Should().Be(5.9);
        first.ValueB.Should().Be(5.5);
        first.Unit.Should().Be("miljoner invånare");
        first.ValueA.Should().BeGreaterThan(first.ValueB, "Mer = sakA holds the larger value");

        pack.Questions.Should().OnlyContain(q =>
            q.QuestionText.Length > 0 && q.ItemA.Length > 0 && q.ItemB.Length > 0 && q.Unit.Length > 0);
    }
}

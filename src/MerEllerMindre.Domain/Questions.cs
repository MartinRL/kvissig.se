using System.Globalization;
using System.Text;

namespace MerEllerMindre.Domain;

/// <summary>
/// A single question card. Compares two things (ItemA, ItemB), each with a hidden
/// raw value (ValueA, ValueB) on a shared Unit. Carries NO precomputed answer —
/// ScoreQuestion derives direction + normalized difference from the raw values.
/// Convention: Mer = ItemA holds the larger value (the author phrases the sentence so).
/// </summary>
public record Question(
    string QuestionText,
    string ItemA,
    string ItemB,
    double ValueA,
    double ValueB,
    string Unit
);

/// <summary>
/// A deck of question cards. PackId is a filename slug (e.g. "mer-eller-mindre").
/// </summary>
public record QuestionPack(
    string PackId,
    string Name,
    IReadOnlyList<Question> Questions
)
{
    public int QuestionCount => Questions.Count;
}

/// <summary>
/// Parses a Swedish-Excel CSV (';'-separated, ','-decimal, sv-SE) into a QuestionPack.
/// Pure string processing — no IO — so it lives in the functional core and is unit
/// testable without files. The file-system catalog (imperative shell) reads the bytes
/// and hands the text here. Malformed input fails fast with a clear message.
/// </summary>
public static class QuestionPackCsvParser
{
    private static readonly CultureInfo SvSe = CultureInfo.GetCultureInfo("sv-SE");

    private const string ColQuestion = "fråga";
    private const string ColItemA = "sakA";
    private const string ColItemB = "sakB";
    private const string ColValueA = "värdeA";
    private const string ColValueB = "värdeB";
    private const string ColUnit = "enhet";

    public static QuestionPack Parse(string slug, string text)
    {
        var rows = ReadRows(StripBom(text));
        if (rows.Count == 0)
            throw new FormatException($"Question pack '{slug}' is empty (no header row).");

        var header = rows[0];
        var index = MapColumns(slug, header);

        var questions = new List<Question>();
        for (var r = 1; r < rows.Count; r++)
        {
            var fields = rows[r];
            if (IsBlankRow(fields))
                continue;

            questions.Add(new Question(
                QuestionText: Field(slug, fields, index, ColQuestion, r),
                ItemA: Field(slug, fields, index, ColItemA, r),
                ItemB: Field(slug, fields, index, ColItemB, r),
                ValueA: ParseValue(slug, Field(slug, fields, index, ColValueA, r), ColValueA, r),
                ValueB: ParseValue(slug, Field(slug, fields, index, ColValueB, r), ColValueB, r),
                Unit: Field(slug, fields, index, ColUnit, r)
            ));
        }

        if (questions.Count == 0)
            throw new FormatException($"Question pack '{slug}' has a header but no question rows.");

        return new QuestionPack(slug, Deslug(slug), questions);
    }

    private static Dictionary<string, int> MapColumns(string slug, IReadOnlyList<string> header)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
            index[header[i].Trim()] = i;

        foreach (var required in new[] { ColQuestion, ColItemA, ColItemB, ColValueA, ColValueB, ColUnit })
        {
            if (!index.ContainsKey(required))
                throw new FormatException(
                    $"Question pack '{slug}' is missing required column '{required}'. " +
                    $"Expected headers: {ColQuestion};{ColItemA};{ColItemB};{ColValueA};{ColValueB};{ColUnit}");
        }

        return index;
    }

    private static string Field(string slug, IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> index, string column, int row)
    {
        var col = index[column];
        if (col >= fields.Count)
            throw new FormatException($"Question pack '{slug}' row {row} is missing column '{column}'.");
        return fields[col].Trim();
    }

    private static double ParseValue(string slug, string raw, string column, int row)
    {
        if (double.TryParse(raw, NumberStyles.Float, SvSe, out var value))
            return value;
        throw new FormatException(
            $"Question pack '{slug}' row {row} column '{column}' has an unparseable number '{raw}' (expected sv-SE decimal, e.g. 5,9).");
    }

    private static string Deslug(string slug)
    {
        var spaced = slug.Replace('-', ' ').Trim();
        if (spaced.Length == 0)
            return slug;
        return char.ToUpper(spaced[0], SvSe) + spaced[1..];
    }

    private static string StripBom(string text) =>
        text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;

    private static bool IsBlankRow(IReadOnlyList<string> fields) =>
        fields.All(f => f.Trim().Length == 0);

    /// <summary>
    /// RFC4180-style reader (delimiter ';'): honours quoted fields that may contain
    /// ';', ',', newlines, and escaped '""'. Recognises \r\n, \n and \r record breaks.
    /// </summary>
    private static List<List<string>> ReadRows(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var sawAny = false;

        void EndField()
        {
            row.Add(field.ToString());
            field.Clear();
        }

        void EndRow()
        {
            EndField();
            rows.Add(row);
            row = [];
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            sawAny = true;

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ';':
                    EndField();
                    break;
                case '\r':
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                    EndRow();
                    break;
                case '\n':
                    EndRow();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        // Flush a trailing record that did not end with a newline.
        if (field.Length > 0 || row.Count > 0)
            EndRow();
        else if (sawAny && rows.Count == 0)
            EndRow();

        return rows;
    }
}

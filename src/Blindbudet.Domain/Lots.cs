using System.Globalization;
using System.Text;

namespace Blindbudet.Domain;

/// <summary>
/// A single auction lot. Carries a hidden <see cref="TrueWorth"/> (the exact author figure,
/// decimal so sv-SE magnitudes round-trip without binary drift) and NO precomputed result —
/// RevealLot derives winner/pricePaid/profit from the bids + trueWorth at reveal.
/// </summary>
public record Lot(
    string Description,
    decimal TrueWorth,
    string Unit
);

/// <summary>
/// A deck of auction lots. PackId is a filename slug (e.g. "blindbudet").
/// </summary>
public record AuctionPack(
    string PackId,
    string Name,
    IReadOnlyList<Lot> Lots
)
{
    public int LotCount => Lots.Count;
}

/// <summary>
/// Parses a Swedish-Excel CSV (';'-separated, ','-decimal, sv-SE) into an AuctionPack.
/// Headers: beskrivning;santVärde;tema;enhet -> Lot { description, trueWorth, unit } (the
/// 'tema' column is metadata for authoring/reporting, not carried onto the lot). Pure string
/// processing — no IO — so it lives in the functional core.
///
/// ponytail: re-implemented (NOT reusing MEM's QuestionPackCsvParser) — that parser is
/// header-mapped to the 7-column MEM schema and produces Question, a different shape. Sharing
/// would need a Kernel extraction; defer to game #3 (n=2). Same RFC4180 reader shape.
/// </summary>
public static class AuctionPackCsvParser
{
    private static readonly CultureInfo SvSe = CultureInfo.GetCultureInfo("sv-SE");

    private const string ColDescription = "beskrivning";
    private const string ColTrueWorth = "santVärde";
    private const string ColUnit = "enhet";

    public static AuctionPack Parse(string slug, string text)
    {
        var rows = ReadRows(StripBom(text));
        if (rows.Count == 0)
            throw new FormatException($"Auction pack '{slug}' is empty (no header row).");

        var header = rows[0];
        var index = MapColumns(slug, header);

        var lots = new List<Lot>();
        for (var r = 1; r < rows.Count; r++)
        {
            var fields = rows[r];
            if (IsBlankRow(fields))
                continue;

            lots.Add(new Lot(
                Description: Field(slug, fields, index, ColDescription, r),
                TrueWorth: ParseValue(slug, Field(slug, fields, index, ColTrueWorth, r), ColTrueWorth, r),
                Unit: Field(slug, fields, index, ColUnit, r)
            ));
        }

        if (lots.Count == 0)
            throw new FormatException($"Auction pack '{slug}' has a header but no lot rows.");

        return new AuctionPack(slug, Deslug(slug), lots);
    }

    private static Dictionary<string, int> MapColumns(string slug, IReadOnlyList<string> header)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
            index[header[i].Trim()] = i;

        foreach (var required in new[] { ColDescription, ColTrueWorth, ColUnit })
        {
            if (!index.ContainsKey(required))
                throw new FormatException(
                    $"Auction pack '{slug}' is missing required column '{required}'. " +
                    $"Expected headers: {ColDescription};{ColTrueWorth};tema;{ColUnit}");
        }

        return index;
    }

    private static string Field(string slug, IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> index, string column, int row)
    {
        var col = index[column];
        if (col >= fields.Count)
            throw new FormatException($"Auction pack '{slug}' row {row} is missing column '{column}'.");
        return fields[col].Trim();
    }

    private static decimal ParseValue(string slug, string raw, string column, int row)
    {
        if (decimal.TryParse(raw, NumberStyles.Number, SvSe, out var value))
            return value;
        throw new FormatException(
            $"Auction pack '{slug}' row {row} column '{column}' has an unparseable number '{raw}' (expected sv-SE decimal, e.g. 1250,5).");
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
    /// RFC4180-style reader (delimiter ';'): honours quoted fields that may contain ';',
    /// ',', newlines, and escaped '""'. Recognises \r\n, \n and \r record breaks.
    /// </summary>
    private static List<List<string>> ReadRows(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();

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
            if (c == '"') { i = ReadQuoted(text, i + 1, field); continue; }
            if (c == ';') { EndField(); continue; }
            if (IsLineBreak(c)) { i = SkipPairedLf(text, i); EndRow(); continue; }
            field.Append(c);
        }

        // Flush a trailing record with no closing newline...
        if (field.Length > 0 || row.Count > 0)
            EndRow();
        // ...or a lone quoted/empty field that produced no row at all.
        else if (text.Length > 0 && rows.Count == 0)
            EndRow();

        return rows;
    }

    private static bool IsLineBreak(char c) => c == '\n' || c == '\r';

    // '\r\n' is one record break: if at '\r' followed by '\n', return the '\n' index so the loop skips it.
    private static int SkipPairedLf(string text, int i) =>
        text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? i + 1 : i;

    // Reads a quoted field starting just after its opening quote; "" is an escaped quote.
    // Returns the index of the closing quote (or the last char, if unterminated) so the
    // caller's loop advances past it.
    private static int ReadQuoted(string text, int start, StringBuilder field)
    {
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] != '"')
            {
                field.Append(text[i]);
                continue;
            }
            if (i + 1 < text.Length && text[i + 1] == '"')
            {
                field.Append('"');
                i++;
                continue;
            }
            return i;
        }
        return text.Length - 1;
    }
}

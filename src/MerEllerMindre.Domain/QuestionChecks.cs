namespace MerEllerMindre.Domain;

/// <summary>
/// Playability checks the fact/language reviewers don't catch. Pure, no IO — one source
/// shared by tools/pack.cs (advisory) and the curated-pack tests (hard assertion).
/// </summary>
public static class QuestionChecks
{
    // sv-SE qualifier tokens that don't name the entity itself (articles, part/whole words).
    private static readonly HashSet<string> Qualifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "ett", "ena", "den", "det", "hela", "hel", "halva", "vänstra", "högra",
        "i", "sin", "helhet", "del", "av", "totala", "samtliga", "all", "allt",
    };

    private static readonly HashSet<string> WholeQualifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "hela", "helhet", "totala", "samtliga",
    };

    /// <summary>
    /// Flags cards whose two items name the same base entity (or one a part of the other),
    /// making the comparison a part-vs-whole tautology with no real guess. The reliable
    /// signal is an explicit whole-word ("hela ögongloben", "hela hjärnan") on one side
    /// that shares a stem with the other ("ögats", "hjärnhalvan").
    /// </summary>
    public static (bool flagged, string reason) SameEntitySmell(Question q)
    {
        // ponytail: whole-qualifier gate only. A general "shared stem >=N" rule was tried and
        // produced 10 false positives (Grönlandshaj/Grönlandsval, Sydneyoperan/Sydney Harbour
        // Bridge, Rekordpumpa/Rekordvattenmelon, …) with ZERO real catches: distinct entities
        // routinely share a modifier prefix, so prefix length can't tell them from a real
        // part-vs-whole. The whole-word is the only honest signal. Upgrade path: add a curated
        // part-vs-whole word list if a tautology without a whole-word ever slips in.
        if (!HasWholeQualifier(q.ItemA) && !HasWholeQualifier(q.ItemB))
            return (false, "");

        var tokensA = Tokens(q.ItemA);
        var tokensB = Tokens(q.ItemB);
        foreach (var a in tokensA)
            foreach (var b in tokensB)
            {
                // Gated by the rare whole-qualifier, so a short (>=2) stem is safe.
                // "ögats"/"ögongloben" only share 2 chars; "hjärnhalvan"/"hjärnan" share 5.
                var cp = CommonPrefix(a, b);
                if (cp >= 2)
                    return (true, $"whole vs part, shared stem '{a[..cp]}'");
            }

        return (false, "");
    }

    private static List<string> Tokens(string item) =>
        item.ToLowerInvariant()
            .Split([' ', '-', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !Qualifiers.Contains(t))
            .ToList();

    private static bool HasWholeQualifier(string item) =>
        item.ToLowerInvariant()
            .Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Any(WholeQualifiers.Contains);

    private static int CommonPrefix(string a, string b)
    {
        var n = Math.Min(a.Length, b.Length);
        var i = 0;
        while (i < n && a[i] == b[i]) i++;
        return i;
    }
}

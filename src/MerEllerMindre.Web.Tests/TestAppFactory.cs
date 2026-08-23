using System.Text;
using MerEllerMindre.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TankTillTusen.Domain;

namespace MerEllerMindre.Web.Tests;

/// <summary>
/// Pins the integration tests to a small fixed 2-card pack so they stay deterministic
/// regardless of the production catalog's size. A pack with &lt;= 21 cards is used whole
/// in file order (Decider.FullGameSize guard), so Q0/Q1 are exactly the two cards
/// written below. Without this the balanced selection would shuffle 21 of the ~350-card
/// production pack and the per-card assertions (Danmark, Sverige) would be non-deterministic.
/// </summary>
public class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _packsDir;
    private readonly string _auctionPacksDir;

    /// <summary>
    /// Shifts the Tank stub clock forward, letting a test expire a round's 60 s deadline
    /// without sleeping. Tests that set it must reset it (shared class fixture).
    /// </summary>
    public TimeSpan TankClockSkew { get; set; }

    /// <summary>
    /// Deterministic 2-puzzle set (the spec's puzzle0/puzzle1 fixtures): [10,10]→100 and
    /// [5,20]→100, both sampled by 0×1. The stub generator ignores the posted roundCount,
    /// so every test game is exactly 2 rounds with known exact/miss solutions.
    /// </summary>
    private static IReadOnlyList<Puzzle> TestPuzzles
    {
        get
        {
            var sample = new Solution([new Step(0, Operator.Mul, 1)], AnswerIndex: 2);
            return [new Puzzle([10, 10], 100, sample), new Puzzle([5, 20], 100, sample)];
        }
    }

    public TestAppFactory()
    {
        _packsDir = Directory.CreateTempSubdirectory("mem-test-packs").FullName;
        // sv-SE dialect: ';' separator, ',' decimal. Q0 = Danmark/Norge, Q1 = Sverige/Norge.
        var csv =
            "fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga\n" +
            "Har Danmark större eller mindre befolkning än Norge?;Danmark;Norge;5,9;5,5;miljoner invånare;Hur många miljoner invånare skiljer det?\n" +
            "Är Sveriges yta större eller mindre än Norges?;Sverige;Norge;450295;385207;km²;Hur många km² skiljer det?\n";
        File.WriteAllText(Path.Combine(_packsDir, "alla-aldrar.csv"), csv, new UTF8Encoding(false));

        // A tiny logo-mode pack (loggor-* prefix) for the logo-rendering test. Q0 = Volvo/Ericsson,
        // companies whose PNGs ship in the Domain corpus, so LogoCatalog.UrlFor resolves.
        var logoCsv =
            "fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga\n" +
            "Vilket företag hade störst omsättning 2023?;Volvo;Ericsson;473;263;miljarder kronor;Hur många miljarder kronor skiljer det?\n" +
            "Vilket företag hade störst omsättning 2023?;H&M;Electrolux;236;135;miljarder kronor;Hur många miljarder kronor skiljer det?\n";
        File.WriteAllText(Path.Combine(_packsDir, "loggor-mini-1.csv"), logoCsv, new UTF8Encoding(false));

        // A fixed 2-lot Blindbudet pack. The slug carries NO "mini" marker, so the Decider
        // plays the whole pack in file order (no sampling) — lot 0 = Everest 8849, lot 1 =
        // equator 40075 — keeping the auction characterization tests deterministic.
        _auctionPacksDir = Directory.CreateTempSubdirectory("bb-test-packs").FullName;
        var auctionCsv =
            "beskrivning;santVärde;tema;enhet\n" +
            "Höjden på Mount Everest över havet;8849;Geografi;meter\n" +
            "Jordens omkrets vid ekvatorn;40075;Geografi;km\n";
        File.WriteAllText(Path.Combine(_auctionPacksDir, "testauktion.csv"), auctionCsv, new UTF8Encoding(false));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<FileSystemQuestionPackCatalog>();
            services.AddSingleton(new FileSystemQuestionPackCatalog(_packsDir));
            services.RemoveAll<FileSystemAuctionPackCatalog>();
            services.AddSingleton(new FileSystemAuctionPackCatalog(_auctionPacksDir));
            services.RemoveAll<TankApplicationService>();
            services.AddSingleton(new TankApplicationService(TankContext.Default with
            {
                Now = () => DateTimeOffset.UtcNow + TankClockSkew,
                GeneratePuzzles = (_, _) => TestPuzzles
            }));
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        if (Directory.Exists(_packsDir))
            Directory.Delete(_packsDir, recursive: true);
        if (Directory.Exists(_auctionPacksDir))
            Directory.Delete(_auctionPacksDir, recursive: true);
    }
}

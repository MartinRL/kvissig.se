using System.Text;
using MerEllerMindre.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MerEllerMindre.Web.Tests;

/// <summary>
/// Pins the integration tests to a small fixed 2-card pack so they stay deterministic
/// regardless of the production catalog's size. A pack with &lt;= 21 cards is used whole
/// in file order (Decider.FullGameSize guard), so Q0/Q1 are exactly the two cards
/// written below. Without this the balanced selection would shuffle 21 of the ~350-card
/// production pack and the per-card assertions (Danmark, Sverige) would be non-deterministic.
/// </summary>
public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _packsDir;

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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<FileSystemQuestionPackCatalog>();
            services.AddSingleton(new FileSystemQuestionPackCatalog(_packsDir));
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_packsDir))
            Directory.Delete(_packsDir, recursive: true);
    }
}

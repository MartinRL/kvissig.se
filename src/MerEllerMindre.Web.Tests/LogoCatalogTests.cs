using AwesomeAssertions;
using MerEllerMindre.Web.Infrastructure;
using Xunit;

namespace MerEllerMindre.Web.Tests;

public class LogoCatalogTests
{
    [Fact]
    public void UrlFor_ResolvesKnownNamesOnlyWhenThePngIsOnDisk()
    {
        var dir = Directory.CreateTempSubdirectory("logo-cat").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "logos.csv"),
                "slug,name,domain,origin\nvolvo,Volvo,volvo.com,se\nericsson,Ericsson,ericsson.com,se\n");
            Directory.CreateDirectory(Path.Combine(dir, "se"));
            // Volvo's PNG exists; Ericsson's does not.
            File.WriteAllBytes(Path.Combine(dir, "se", "volvo.png"), [0x89, 0x50, 0x4e, 0x47]);

            var catalog = new LogoCatalog(dir);

            catalog.UrlFor("Volvo").Should().Be("/logos/se/volvo.png");
            catalog.UrlFor("Ericsson").Should().BeNull();   // row present, PNG missing
            catalog.UrlFor("Nope").Should().BeNull();         // unknown name
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

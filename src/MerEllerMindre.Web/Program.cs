using MerEllerMindre.Web;

var builder = WebApplication.CreateBuilder(args);

// File-based Quiz catalog: load every data/packs/*.csv at startup (fail-fast). The packs
// are domain reference data copied to output beside the Domain assembly (see its csproj).
var packsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "packs");
builder.Services.AddSingleton(new FileSystemQuestionPackCatalog(packsDirectory));

var app = builder.Build();

app.MapGet("/", () => "Mer eller Mindre");

app.Run();

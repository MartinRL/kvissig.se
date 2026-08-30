# Analys: extrahera .NET-emlang till xmlang-repot som NuGet-paket

Datum: 2026-08-29. Status: beslut låsta, fas 0 (detta dokument). Ingen kod flyttas i denna iteration.

## 1. Sammanfattning och låsta beslut

.NET-emlang-stacken bor idag i kvissig: `src/Emlang.CodeGen` (parser + emitters, ~779 LOC),
`src/Emlang.Generators` (Roslyn source generator, 83 LOC) och `src/Emlang.CodeGen.Tests`
(511 LOC). Samtidigt skeppar NuGet-paketet `Xmlang` 0.4.1 en EGEN em-parser (`EmParser`,
`xmlang/src/Xmlang/EmParser.cs`) som XmCatalog-runtimen använder. Två .NET-em-parsers
existerar alltså i två repon.

Målet: emlang blir egna NuGet-paket i xmlang-repot, med egen release-pipeline, och en
.NET-CLI `em` som på sikt ersätter Go-CLI:n (github.com/emlang-project/emlang v1.0.0)
till 100 %.

Låsta beslut (user):

- **Hem: github.com/MartinRL/xmlang behålls** (inget repo-rename). Emlang flyttar in som
  syskon-src-träd. Motiv: xmlang kan inte leva utan emlang, gemensam historik och pipeline-infra.
- **Topologi: Xmlang beror på Emlang.** `EmParser` flyttar UT ur Xmlang-paketet IN i
  Emlang-paketet. Resultat: EN .NET-em-parser som källa för både codegen och xm-runtime.
- **Fem NuGet-paket i repot:** `Emlang`, `Emlang.Generators`, `Emlang.Cli` (nya) plus
  `Xmlang`, `Xmlang.Cli` (befintliga).
- **Två release-pipelines** (tag `emlang-v*` respektive `xmlang-v*`), emlang står på egna ben.
- **.NET-CLI:n = 100 % klon av Go-CLI:n** (parse/lint/fmt/diagram/repl/init/version/help),
  fasindelad (§5).
- En framtida gemensam viewer lämnas utanför denna iteration.

## 2. Parser-unifiering

**Ett lib-paket `Emlang`**, multi-target `netstandard2.0;net10.0` (ns2.0 är analyzer-host-kravet:
Emlang.Generators laddar lib-dll:en inne i kompilatorprocessen, samma motiv som dagens
kommentar i `src/Emlang.Generators/Emlang.Generators.csproj:4-7`). YamlDotNet 16.3.0 som
dependency är OK: `EmParser` använder redan `YamlDotNet.RepresentationModel`
(`xmlang/src/Xmlang/EmParser.cs:4`), samma teknik som `SpecModel` i Emlang.CodeGen.

Tre namespaces i samma assembly:

| Namespace | Innehåll | Härkomst |
|---|---|---|
| `Emlang` | `EmParser`, `EmSpec`, `EmElement`, `EmField` | flyttas ordagrant från `xmlang/src/Xmlang/EmParser.cs`, bara namespace-byte `Xmlang` → `Emlang` |
| `Emlang.CodeGen` | `SpecModel`/`TestModel` + `SurfaceEmitter`/`TestsEmitter`/`DeciderEmitter` | flyttas från kvissigs `src/Emlang.CodeGen`; emitters ligger i LIB, testbara utan Roslyn |
| `Emlang.Linting` | CLI-fas 1-lintreglerna | nyskrivet, nåbart för framtida xm-integration |

**SemVer:** Xmlang 0.4.1 → **0.5.0**. Pre-1.0-breaking är OK, ingen type-forwarding behövs:
kvissig är enda kända konsumenten och tar using-bytet i `src/MerEllerMindre.Web/Xm/XmCatalog.cs:1`
(`using Xmlang;` → även `using Emlang;`) rakt av vid cutover.

**EmParser och SpecModel förblir två parsers över samma YAML dag 1.** De har olika behov:
EmParser levererar rå camelCase-yta som xm-referenser resolvar mot (dokumenterat i
`EmParser.cs:10-13`), SpecModel levererar PascalCase + C#-typmappning för emitters.
Konsolidering är ett uttalat framtida steg inne i samma paket, inte en flyttblockerare.

## 3. GameManifest → konsument-konfiguration

Dagens `GameManifest.cs` (`src/Emlang.CodeGen/GameManifest.cs:11-55`) är en central hårdkodad
lista med tre spel; generatorn matchar spec-filnamn mot listan
(`src/Emlang.Generators/EmlangRecordsGenerator.cs:31-32`). Det fungerar inte i ett paket:
paketet kan inte känna till konsumentens spel.

**Rekommendation: konvention + ETT metadata-attribut på AdditionalFiles:**

```xml
<AdditionalFiles Include="..\..\specs\mer-eller-mindre.em.yaml" EmlangPrefix="Game" />
```

Läses via `AnalyzerConfigOptionsProvider.GetOptions(additionalText)` →
`build_metadata.AdditionalFiles.EmlangPrefix`. Prefixet ger unionsnamnen
`{Prefix}Command`/`{Prefix}Event`/`{Prefix}Error`/`{Prefix}State`/`{Prefix}Context`, vilket
täcker alla tre spel exakt (Game/Auction/Tank, se manifest-instanserna i
`GameManifest.cs:34,43,52`).

- **Namespace** hämtas ur `build_property.RootNamespace` (verifiera compiler-visibility i
  fas 1; annars räcker en `CompilerVisibleProperty`-rad i paketets props-fil). Tests-mode
  (dagens `EmlangEmit=tests`-gate, `EmlangRecordsGenerator.cs:19-22`) strippar `.Tests`-suffixet,
  motsvarande dagens `TestsNamespace` (`GameManifest.cs:25`). Optional `EmlangNamespace`-metadata
  som override.
- Central manifest-lista + filnamnsmatch försvinner; `GameManifest` ersätts av ett litet
  `EmitTarget(Namespace, Prefix)`-record inne i generatorn.
- **Bekräftat i koden:** manifest-fälten `SpecPath`-som-repo-relativ-path,
  `CommandsFile`/`EventsFile`/`ErrorsFile` samt `RepoRoot.Locate` (`GameManifest.cs:13,15-17,59-68`)
  används ENDAST av testharnessen (SurfaceComparer-rundturen), aldrig av generatorn, som bara
  konsumerar filnamn + unionsnamn + namespace. De följer med till testprojektet (§6), inte paketet.

## 4. NuGet-packaging av generatorn

**Två paket, inte kombo:**

- `Emlang` = rent lib-paket (vanlig `lib/`-layout, YamlDotNet som vanlig NuGet-dep).
- `Emlang.Generators` = rent analyzer-paket: `IncludeBuildOutput=false`, allt under
  `analyzers/dotnet/cs/`: `Emlang.Generators.dll` + `Emlang.dll` + `YamlDotNet.dll`. Det är
  pack-time-motsvarigheten till dagens `GetDependencyTargetPaths`-target
  (`src/Emlang.Generators/Emlang.Generators.csproj:22-30`) som idag lägger CodeGen-dll +
  YamlDotNet som Analyzer-items hos konsumenten. `DevelopmentDependency=true` så att
  konsumenter får `PrivateAssets=all` som default.
- Roslyn-pinnen `Microsoft.CodeAnalysis.CSharp` 4.14.0 `PrivateAssets=all` behålls
  (csproj:14, CS9057-motivet i csproj:4-7); Roslyn-dll:er hamnar aldrig i paketet.

Motiv för splitten: lib-konsumenter (t.ex. framtida verktyg som bara vill parsa) ska inte få
en analyzer injicerad i sin kompilering, och Domain-projekt ska inte få runtime-dll:er i
output (arch-testets nolldep-krav, se §8 punkt 3).

## 5. CLI-fasindelning (`em`, PackageId `Emlang.Cli`)

Global tool, kommandonamn `em`. Ingen PATH-krock: Go-binären heter `emlang`.

- **Fas 1: `lint` + `parse` + `version`/`help`.** Golden-testas mot Go-CLI:ns output på
  kvissigs tre specs (mer-eller-mindre/blindbudet/tank-till-tusen `.em.yaml`). Detta täcker
  kvissigs enda faktiska Go-användning idag (lint).
- **Fas 2: `fmt`.** OBS: YamlDotNet RepresentationModel bevarar inte kommentarer vid
  round-trip → kräver en egen kanonisk emitter. Verifiera först hur Go-CLI:n hanterar
  kommentarer innan ambitionsnivån låses.
- **Fas 3: `diagram`** (`-o`/`--serve`/`--address`/`--port`, HTML-template som embedded resource).
- **Fas 4: `repl`, `init`, `-c/--config`, stdin `-`.** 100 %-målet är låst men YAGNI-fasat:
  varje fas släpps när den behövs, inte i förväg.

## 6. Test-split

**Flyttar till `xmlang/tests/Emlang.Tests`:** SpecModelTests, TestModelTests,
TestsEmitterTests, SurfaceComparerTests (idag i `src/Emlang.CodeGen.Tests/`), plus
`SurfaceComparer.cs`/`CodeSurface.cs` som test-only-harness, de flyttar in i TESTPROJEKTET,
inte paketet (de finns idag i `src/Emlang.CodeGen/` men konsumeras bara av tester).
Kvissigs tre riktiga specs kopieras som **frysta fixtures** till `tests/Emlang.Tests/fixtures/`,
de uppdateras medvetet, inte automatiskt, när kvissigs specs ändras.

**Kvissig: radera `src/Emlang.CodeGen.Tests` helt vid cutover.** Kompilatorn ÄR
konsumentkontraktet:

- CS8795 = ny `c:`/`e:` i spec utan Decider.Impl-kropp (ADR 018-seamen).
- CS0117 = GWT-fixture-värde utan case i den handskrivna `Fixtures`-klassen.
- Gröna SpecTests.g.cs = spec-GWT:erna håller.

Ett test som `dotnet build` redan bevisar är ett test för mycket.

## 7. Versionering och release i mono-repot

- **Familje-dirar med egen Version:** `src/emlang/Directory.Build.props` respektive
  `src/xmlang/Directory.Build.props`; rotens `Directory.Build.props` behåller delad metadata
  (Authors, License, RepositoryUrl) men tappar `<Version>`.
- **`release-emlang.yml`** (tag `emlang-v*`): guard mot `src/emlang/Directory.Build.props`,
  test + pack + Trusted Publishing (OIDC), samma mall som dagens `release.yml`.
- **`release-xmlang.yml`** (tag `xmlang-v*`): behåller dagens spec-frontmatter-guard
  (`xmlang/.github/workflows/release.yml:26-30`) men mot värdet `xmlang-vX.Y.Z` i
  `xmlang-spec.md`. **Emlang guardar INTE mot Go-specens version**: egen SemVer, README
  deklarerar "implements emlang spec 1.0.0".
- Xmlang → Emlang inom repot = **ProjectReference**; `dotnet pack` översätter den till en
  exakt versionerad NuGet-dependency. Vid samtidiga ändringar i båda familjerna släpps
  emlang-taggen FÖRE xmlang-taggen så att Xmlang-paketets dep finns på nuget.org.

## 8. Kvissig-migrationssteg (senare iteration, fas 2)

1. Sex csproj (MerEllerMindre/Blindbudet/TankTillTusen × Domain + Domain.Tests): byt
   Analyzer-ProjectReference mot
   `<PackageReference Include="Emlang.Generators" PrivateAssets="all" />` +
   `EmlangPrefix`-metadata på AdditionalFiles-raden. Tests-projekten behåller
   `<EmlangEmit>tests</EmlangEmit>`.
2. Radera `src/Emlang.CodeGen`, `src/Emlang.Generators`, `src/Emlang.CodeGen.Tests`;
   uppdatera slnx.
3. Arch-tester ×3: `Domain_project_has_no_dependencies`
   (`src/MerEllerMindre.Domain.Tests/ArchitectureTests.cs:60-78` + syskonen) förbjuder idag
   ALLA `<PackageReference` (rad 65) och tillåter bara Analyzer-wirade ProjectReferences
   (rad 69-74). Regeln får en allowlist för analyzer-PackageReference
   (`Emlang.Generators` med `PrivateAssets="all"`).
4. `codehealth.sh`: ta bort `Emlang\.(CodeGen|Generators)` ur scope-regexen
   (`.claude/hooks/codehealth.sh:49`).
5. Web: `Xmlang` 0.4.1 → 0.5.0 + using-byte i `XmCatalog.cs`.
6. Ny ADR + uppdatera CLAUDE.md/MEMORY/emlang-codegen-topicfilen; docs byter Go-lint-instruktionen
   mot `dotnet tool install -g Emlang.Cli` (fas 3, när `em lint` finns).

## 9. Risker och öppna frågor

- **Dialekt-drift mot Go:** .NET-parsers är tolerantare än Go-linten. Hållning: parsers
  förblir toleranta (Postels lag), `em lint` bär strängheten, golden-diff mot Go-CLI:n
  håller dem i synk.
- **C#15 `public union` i genererad kod** kräver `LangVersion=preview` hos konsumenten.
  Dokumenterat paketkrav; xmlang-repot självt emittar bara text, net10-SDK räcker för att
  bygga paketen.
- **YamlDotNet-versionkrock i analyzer-load-context:** om konsumentens andra analyzers
  laddar en annan YamlDotNet-version kan kompilator-processen få en krock. Känd
  Roslyn-begränsning, låg sannolikhet, dokumenteras i README. ILRepack = YAGNI tills det
  faktiskt händer.
- **NuGet-namnen:** `Emlang` är LEDIGT (404 på nuget.org verifierat 2026-08-29). Reservera
  `Emlang`/`Emlang.Generators`/`Emlang.Cli` tidigt i fas 1. Paketbeskrivning:
  ".NET implementation of the emlang spec", formuleringen är varumärkeshänsyn mot
  emlang-project, inte teknisk.
- **Fixtures-seamen** (den human-ägda `Fixtures`-klassen som CS0117-orakel) blir paketets
  dokumenterade konvention, den är idag implicit kvissig-kunskap.

## 10. Repo-slutstruktur (xmlang)

```
xmlang/
├── Directory.Build.props          # delad metadata, ingen Version
├── xmlang-spec.md                 # release-frontmatter: xmlang-vX.Y.Z
├── src/
│   ├── emlang/
│   │   ├── Directory.Build.props  # emlang-familjens Version
│   │   ├── Emlang/                # lib: Emlang + Emlang.CodeGen + Emlang.Linting
│   │   ├── Emlang.Generators/     # analyzer-paketet
│   │   └── Emlang.Cli/            # global tool `em`
│   └── xmlang/
│       ├── Directory.Build.props  # xmlang-familjens Version
│       ├── Xmlang/                # utan EmParser, ProjectReference → Emlang
│       └── Xmlang.Cli/            # global tool `xm`
├── tests/
│   ├── Emlang.Tests/              # + fixtures/ (frysta kvissig-specs) + SurfaceComparer-harness
│   └── Xmlang.Tests/
└── .github/workflows/
    ├── ci.yml
    ├── release-emlang.yml         # tag emlang-v*
    └── release-xmlang.yml         # tag xmlang-v*
```

## 11. Migrationssekvens i faser

- **Fas 0 (denna iteration):** detta analysdokument. Inget annat.
- **Fas 1 (endast xmlang-repot):** familje-dirar, EmParser → Emlang, CodeGen-flytt med
  EmitTarget/metadata-mekanismen (§3), test-flytt + frysta fixtures, två pipelines.
  Release `emlang-v0.1.0` + `xmlang-v0.5.0`. Kvissig orörd: Xmlang 0.4.1 + lokal källkod =
  medveten temporär dubbel källa.
- **Fas 2 (kvissig cutover, EN PR):** §8-stegen.
- **Fas 3:** `em lint` släpps; kvissig-docs byter Go-CLI mot `Emlang.Cli`.
- **Fas 4+:** `fmt` → `diagram` → `repl`/`init` mot 100 %-klonen, varje steg när behovet
  finns.

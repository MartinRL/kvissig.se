---
status: Accepted
type: architecture
created: 2026-08-30
---

# ADR 020: emlang-codegen konsumeras som NuGet-paket från xmlang-repot

## Context
ADR 016–018 gjorde Commands/Events/Errors, Decider-skeletten och spec-testerna till
byggartefakter av emlang-specarna, genererade av `src/Emlang.CodeGen` +
`src/Emlang.Generators` i det här repot. xmlang extraherades redan 2026-08-23 till
github.com/MartinRL/xmlang (NuGet `Xmlang`), men Xmlang-paketet skeppade en EGEN
em-parser (`EmParser`) medan codegen-stacken bodde kvar här: två .NET-em-parsers och
en generator utan eget liv utanför kvissig.

Analysen i `docs/analysis/emlang-dotnet-extraction.md` låste besluten: emlang blir
egna NuGet-paket i xmlang-repot (Xmlang beror på Emlang, EN parser), med egna
release-pipelines (taggar `emlang-v*` / `xmlang-v*`) och på sikt en .NET-CLI `em`
som ersätter Go-CLI:n.

## Decision
kvissig konsumerar generatorn som NuGet i stället för källkod:

- **`Emlang.Generators` 0.1.0** som analyzer-paket (`PrivateAssets="all"`) i alla
  6 Domain/Tests-projekt. `GameManifest`-mekanismen ersattes av metadata på
  spec-filen: `<AdditionalFiles Include="..." EmlangPrefix="Game" />` (BB: `Auction`,
  TTT: `Tank`); namespace tas från `RootNamespace` (tests-mode strippar `.Tests`).
  `EmlangEmit=tests`-gaten är oförändrad; `CompilerVisibleProperty`-raden behövs inte
  längre (paketets buildTransitive-props levererar den).
- **`src/Emlang.CodeGen`, `src/Emlang.Generators`, `src/Emlang.CodeGen.Tests`
  raderade.** Emitter-testerna + SurfaceComparer-harnessen bor nu i xmlang-repots
  `tests/Emlang.Tests` med kvissigs tre specs som frysta fixtures. Kompilatorn ÄR
  konsumentkontraktet här: CS8795 (saknad Decider.Impl-kropp), CS0117 (saknad
  Fixtures-case) och gröna SpecTests.g.cs.
- **Web: `Xmlang` 0.4.1 → 0.5.0** (EmParser flyttade till Emlang-paketet, som följer
  med som transitiv dep; `XmCatalog.cs` fick `using Emlang;`).
- Arch-testet `Domain_project_has_no_dependencies` tillåter numera exakt EN
  PackageReference: `Emlang.Generators` med `PrivateAssets="all"`. Nolldep-regeln
  vid runtime består.

Go-CLI:n (`emlang lint`) används tills `em lint` (Emlang.Cli, fas 3) är släppt.

## Consequences
- Genererad kod är oförändrad byte för byte (E2E-verifierat mot paketet innan
  cutover): samma unions, partials och spec-tester.
- Versionsbump av generatorn = vanlig NuGet-uppgradering i 6 csproj (Directory
  .Packages.props är YAGNI på n=6 rader).
- Emitter-buggar felsöks/fixas i xmlang-repot; kvissig fångar regressionerna via
  kompilatorkontraktet och spec-testerna.
- codehealth-scopet tappade `Emlang\.(CodeGen|Generators)` (koden bor inte här).

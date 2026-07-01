# Mer eller Mindre

En kviss för flera spelare. Varje fråga jämför två saker (A och B) och spelas som en
*tvåstegsraket*: först gissar alla *mer eller mindre*, sedan *hur stor*
skillnaden är.

Inspirerat av sällskapsspelet [0-100](https://playmig.com/produkter/0-100-vit/).

## Så spelas det — tvåstegsraketen

En fråga avgörs i två steg, med ett litet avslöjande emellan:

1. **Steg 1 – Mer eller mindre.** Alla svarar bara *mer eller mindre*: är A mer eller mindre än B?
   När alla har svarat avslöjas vilket som var mer och **−10-bonusen delas ut direkt** till dem
   som hade rätt. Det avslöjandet är hela poängen med att dela upp raketen.
2. **Steg 2 – Differens.** Alla gissar sedan *hur stor* skillnaden är — den **råa
   skillnaden i kortets egen enhet** (t.ex. 0,3 miljoner invånare eller 220 km), inte ett
   0–100-tal. Systemet känner de dolda värdena och **normaliserar gissningen server-side**
   till 0–100 för poängräkning.

En omgång spelas över flera kort (21, svårighetsbalanserade). **Lägst total vinner.**

## Poängsättning

```
roundScore = |gissad_differens_normaliserad − facit_differens| + (rätt_mer_mindre ? −10 : 0)
```

Både din gissning och facit normaliseras till 0–100 med kortets största värde,
`mx = max(A, B)`:

```
facit    = round(|A − B| / mx × 100)
gissning = min(100, round(rå_gissning / mx × 100))   (samma mx, klampas vid 100)
```

Tänk på de två värdena som två staplar bredvid varandra: lika→0, hälften→50, en
tiondel→90, nästan inget→nära 100. Stora skillnader komprimeras medvetet (2x→50, 10x→90)
— det är de små skillnaderna som är roliga att pricka.

Bonusen −10 landar i **steg 1** (rätt på mer/mindre), differenspoängen i **steg 2**; de
summeras till `roundScore`.

**Räknat exempel** — Danmark 5,9 / Norge 5,5 miljoner invånare:

- `mx = 5,9`
- facit: `round(0,4 / 5,9 × 100) = 7`
- gissning "Mer, 0,3": `round(0,3 / 5,9 × 100) = 5`
- differenspoäng: `|5 − 7| = 2`
- rätt på mer/mindre: `−10`
- **roundScore = −8**

**Lägsta total vinner. Negativa poäng är möjliga** — exakt rätt differens plus rätt
på mer/mindre ger −10, den bästa möjliga rundan. Oavgjort på lägsta total = delad vinst.

## Snabbstart

```bash
dotnet build
dotnet test
dotnet run --project src/MerEllerMindre.Web
```

## Arkitektur

- **Event Sourcing** via Decider-mönstret
- **In-Memory** — inga databaser
- **htmx + 2s-polling**, renderat med Razor Components i statisk SSR (ingen
  SignalR/WebSocket/circuit) — se ADR 007
- **Frågepaket som CSV-kort** i `src/MerEllerMindre.Domain/data/packs`, redigerbara i
  Excel — se ADR 005
- **.NET 11 preview** + C# 15 union-typer för `Result<T>` och domänen
- **emlang-spec** — `specs/mer-eller-mindre-event-model.yaml` är sanningskällan

## Licens

Proprietär — all rights reserved (se LICENSE)

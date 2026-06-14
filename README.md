# Mer eller Mindre

Ett quizspel för flera spelare där man gissar:
1. **Riktning**: Är A mer eller mindre än B?
2. **Differens**: Hur stor är skillnaden? Du gissar den **råa skillnaden i kortets egen
   enhet** (t.ex. antal km eller miljoner invånare) — systemet normaliserar gissningen
   till 0–100 för poängräkning.

Inspirerat av sällskapsspelet [0-100](https://playmig.com/produkter/0-100-vit/).

## Poängsättning

Både din gissning och facit normaliseras till 0–100 med kortets största värde
(`max(A, B)`); din gissning klampas vid 100.

| Händelse | Poäng |
|----------|-------|
| Skillnad mellan din normaliserade gissning och facit | +diff (0–100) |
| Rätt på mer/mindre | **−10 bonus** |

**Lägsta total vinner.** Negativa poäng är möjliga!

## Snabbstart

```bash
dotnet build
dotnet test
dotnet run --project src/MerEllerMindre.Web
```

## Arkitektur

- **Event Sourcing** via Decider-mönstret
- **In-Memory** — inga databaser
- **HTMX + Polling** — enkla realtidsuppdateringar
- **emlang-spec** — `specs/game-flows.yaml` är sanningskällan

## Licens

MIT med icke-kommersiell klausul

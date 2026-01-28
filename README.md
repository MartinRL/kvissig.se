# Mer eller Mindre

Ett quizspel för flera spelare där man gissar:
1. **Riktning**: Är A mer eller mindre än B?
2. **Differens**: Hur stor är skillnaden? (0-100)

Inspirerat av sällskapsspelet [0-100](https://playmig.com/produkter/0-100-vit/).

## Poängsättning

| Händelse | Poäng |
|----------|-------|
| Differens mellan gissning och facit | +diff |
| Rätt på mer/mindre | **-10 bonus** |

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
- **emlang-spec** — `specs/game-flows.em` är sanningskällan

## Licens

MIT med icke-kommersiell klausul

# Mer eller Mindre

Ett quizspel för flera spelare där man gissar om det ena är **mer** eller **mindre** än det andra. Snabbaste rätta svaret vinner rundan.

Optimerat för **sällskapsspel i samma rum** — alla spelar på sin egen enhet, inklusive spelledaren.

## Snabbstart

```bash
# Bygg
dotnet build

# Testa
dotnet test

# Kör
dotnet run --project src/MerEllerMindre.Web
```

## Arkitektur

- **Event Sourcing**: All state härleds från händelser via Decider-mönstret
- **In-Memory**: Ingen databas, spel existerar endast under körning
- **HTMX + Polling**: Enkla realtidsuppdateringar var 2:a sekund
- **emlang-specifikation**: Beteende definieras i `specs/game-flows.em`

## Projektstruktur

```
├── MerEllerMindre.slnx          # .NET 9 solution
├── CLAUDE.md                     # Claude Code-instruktioner
├── specs/
│   ├── game-flows.em            # emlang-specifikation (sanningskälla)
│   └── tasks.md                 # Implementationsuppgifter
├── docs/adr/                    # Architecture Decision Records
├── src/
│   ├── MerEllerMindre.Domain/   # Spelmotor (rena funktioner)
│   └── MerEllerMindre.Web/      # HTMX-webbgränssnitt
└── tests/
    └── MerEllerMindre.Domain.Tests/
```

## Utveckling med Claude Code

Projektet är strukturerat för specifikationsdriven utveckling med Claude Code:

1. **Läs specen**: `specs/game-flows.em` definierar allt spelbeteende
2. **Kolla constitution**: `.claude/constitution.md` definierar kodstandarder
3. **Följ uppgifterna**: `specs/tasks.md` listar implementationsarbete
4. **Kör tester**: Tester härleds från emlang `?test?`-block

### RALPH Loop-kompatibel

```bash
# Med ralph-loop-plugin installerad:
/ralph-loop "Implement the next unchecked task in specs/tasks.md.
Run tests after each change. Output <promise>TASK_COMPLETE</promise> when done."
--max-iterations 20
--completion-promise "TASK_COMPLETE"
```

## Licens

MIT

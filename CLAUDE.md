# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project: Mer eller Mindre

Ett quizspel där spelarna gissar:
1. **Riktning**: Är A mer eller mindre än B?
2. **Differens**: Hur stor är skillnaden? (normaliserad 0-100)

Inspirerat av [0-100](https://playmig.com/produkter/0-100-vit/).

### Poängsättning

```
roundScore = |gissad_diff - faktisk_diff| + (rätt_riktning ? -10 : 0)
```

- Differenspoäng alltid 0-100
- Rätt riktning = -10 bonus
- **Lägsta totala poäng vinner**
- Negativa poäng möjliga (-10 vid rätt riktning + exakt diff)

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter "GameCanBeCreated"
dotnet run --project src/MerEllerMindre.Web
```

## Architecture

### Decider Pattern (Event Sourcing)

```
Evolve: (State, Event) → State
Decide: (State, Command) → Result<Event[]>
```

Both use exhaustive pattern matching.

### Key Files

```
specs/game-flows.em              # Source of truth (emlang spec)
specs/tasks.md                   # Implementation checklist
.claude/constitution.md          # Coding standards
src/MerEllerMindre.Domain/       # Pure domain logic
src/MerEllerMindre.Web/          # HTMX web interface
tests/MerEllerMindre.Domain.Tests/
```

## Constraints (from constitution.md)

**Required:**
- All public types are records
- No exceptions for business logic — use `Result<T>`
- Exhaustive pattern matching (no default cases)

**Forbidden:**
- SignalR, WebSockets, SSE
- Entity Framework, databases
- Blazor

## Workflow

1. **Spec first**: `specs/game-flows.em` defines all behavior
2. **Domain types**: Records matching the spec
3. **Decider**: Update `Evolve` and `Decide`
4. **Tests**: GWT from `?TestName?` blocks
5. **Web**: HTMX endpoints and pages

## Naming

- Commands: verb noun (`CreateGame`, `SubmitGuess`)
- Events: noun past-tense (`GameCreated`, `GuessSubmitted`)
- Errors: descriptive (`GameNotFound`, `InvalidDirection`)

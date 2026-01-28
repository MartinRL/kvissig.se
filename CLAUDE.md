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
Decide: (State, Command, GameContext) → Result<Event[]>
```

- Both use exhaustive switch expressions
- `GameContext` provides external dependencies (ID generators, clock)
- `Result<T>` with `Match()` for success/failure handling
- `Fold()` aggregates events into state

### Domain Structure

```
MerEllerMindre.Domain/
├── Commands.cs    # CreateGame, JoinGame, StartGame, SubmitGuess
├── Events.cs      # GameCreated, PlayerJoined, GuessSubmitted, etc.
├── Errors.cs      # GameNotFound, AlreadyGuessed, DifferenceOutOfRange
├── State.cs       # GameState, Player, GamePhase enum
└── Decider.cs     # Evolve, Decide, Fold, Result<T>, GameContext
```

### Source of Truth

```
specs/game-flows.em              # emlang spec — ALL behavior defined here
specs/tasks.md                   # Implementation checklist
.claude/constitution.md          # Coding standards
```

### emlang Syntax (in game-flows.em)

- `[CommandName]` — Command with properties
- `<Aggregate:EventName>` — Event that occurred
- `!ErrorName!` — Business error
- `?TestName?` — GWT test case (Given: events, When: command, Then: events/error)

## Constraints

**Required:**
- All public types are records
- No exceptions for business logic — use `Result<T>`
- Exhaustive pattern matching (no default/discard cases)
- Collections use `IReadOnlyList<T>`

**Forbidden:**
- SignalR, WebSockets, SSE
- Entity Framework, databases
- Blazor
- `dynamic` or reflection in domain

## Workflow

1. **Spec first**: Update `specs/game-flows.em`
2. **Domain types**: Add records matching the spec
3. **Decider**: Update `Evolve` and `Decide` switches
4. **Tests**: Implement GWT from `?TestName?` blocks
5. **Web**: HTMX endpoints and Razor pages

## Naming

- Commands: verb noun (`CreateGame`, `SubmitGuess`)
- Events: noun past-tense (`GameCreated`, `GuessSubmitted`)
- Errors: descriptive (`GameNotFound`, `InvalidDirection`)

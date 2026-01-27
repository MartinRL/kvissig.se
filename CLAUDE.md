# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project: Mer eller Mindre

A real-time multiplayer quiz game where players guess percentages/numbers (0-100). Closest to the correct answer wins the round. Built for same-room social gaming where everyone plays on their own device.

## Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run single test by name
dotnet test --filter "GameCanBeCreated"

# Run tests with output
dotnet test --logger "console;verbosity=detailed"

# Run web app
dotnet run --project src/MerEllerMindre.Web
```

## Architecture

### Decider Pattern (Event Sourcing)

The domain uses the Decider pattern with two pure functions:

```
Evolve: (State, Event) → State     # Folds events into current state
Decide: (State, Command) → Events  # Validates command, produces new events
```

- **Commands** (`Commands.cs`): User intentions (CreateGame, JoinGame, SubmitGuess)
- **Events** (`Events.cs`): Facts that happened (GameCreated, PlayerJoined, GuessSubmitted)
- **State** (`State.cs`): Current game state derived from folding all events
- **Errors** (`Errors.cs`): Business rule violations (GameNotFound, AlreadyGuessed)

Both `Evolve` and `Decide` use exhaustive pattern matching - compiler must catch missing cases.

### Tech Stack

- .NET 9 minimal APIs with `TreatWarningsAsErrors`
- HTMX for UI (polling every 2s, no WebSockets)
- Questions from CSV file, memoized as records
- In-memory event store per game (no database)
- xUnit + FluentAssertions for tests

### Key Constraints (from `.claude/constitution.md`)

**Required:**
- All public types are records (immutable)
- No exceptions for business logic - use `Result<T>` pattern
- Collections use `IReadOnlyList<T>`
- No mutable domain state

**Forbidden:**
- SignalR, WebSockets, SSE
- Entity Framework, Dapper, raw SQL
- Blazor Server or WASM
- `dynamic` or reflection in domain

## Development Workflow

1. **Spec first**: Read/update `specs/game-flows.em` (source of truth)
2. **Domain types**: Add/update records matching the spec
3. **Decider**: Update `Evolve` and `Decide` switches
4. **Tests**: Implement GWT tests from `?TestName?` blocks in spec
5. **Web**: Update endpoints and Razor pages with HTMX

## emlang Specification

The `specs/game-flows.em` file defines all game behavior. Key syntax:

- `[CommandName]` - Command with properties below
- `<Aggregate:EventName>` - Event that occurred
- `!ErrorName!` - Business error
- `?TestName?` - GWT test case (Given: events, When: command, Then: events/error)

Tests are derived directly from the spec - don't invent new test scenarios.

## Project Structure

```
specs/game-flows.em              # Behavior specification (source of truth)
specs/tasks.md                   # Implementation checklist
.claude/constitution.md          # Coding standards and constraints
docs/adr/                        # Architecture Decision Records
src/MerEllerMindre.Domain/       # Pure domain logic (Decider)
src/MerEllerMindre.Web/          # HTMX web interface
tests/MerEllerMindre.Domain.Tests/
```

## Naming Conventions

- Commands: verb noun (`CreateGame`, `SubmitGuess`)
- Events: noun past-tense (`GameCreated`, `GuessSubmitted`)
- Errors: descriptive (`GameNotFound`, `AlreadyGuessed`)

# Constitution: Mer eller Mindre

## Core Principles

### 1. Specification First
- The `specs/mer-eller-mindre-event-model.yaml` file is the **single source of truth**
- All behavior must be specified in emlang before implementation
- Tests are derived from `tests:` (given/when/then) blocks, not invented

### 2. Functional Core, Imperative Shell
- Domain logic is pure functions (Decider pattern)
- Side effects (HTTP, time, randomness, exceptions, async/await, ...) live at the edges
- No mutable state in the domain layer

### 3. Simplicity Over Sophistication
- Polling over WebSockets
- CSV over database
- In-memory over persistence
- Add complexity only when proven necessary

### 4. Exhaustive Pattern Matching
- All switch expressions must be exhaustive
- Compiler must catch missing cases
- No default/fallback cases that hide bugs

### 5. Immutable by Default
- All public types are records
- No setters, only `with` expressions
- Collections are `IReadOnlyList<T>`

## Code Standards

### Naming
- Commands: verb noun (`CreateGame`, `SubmitGuess`)
- Events: noun past-tense (`GameCreated`, `GuessSubmitted`)  
- Errors: descriptive (`GameNotFound`, `AlreadyGuessed`)

### Error Handling
- No exceptions for business logic — the functional core is total and synchronous
- Business failures are values on the `Result` failure track (Railway Oriented
  Programming); `Result<T>` is a native C# union (see ADR 006)
- emlang `x:` elements map to `Error` cases on the failure track, never `throw`
- Fail fast on infrastructure errors (startup, config)

### Testing
- One test class per GWT scenario group
- emlang `tests:` names are lowercase sentences with spaces (`cannot join
  nonexistent game`) so the diagram renders them as readable sentences
- C# `[Fact]` method names are the deterministic transform of the spec name:
  join words with underscores, capitalize the first word
  (`cannot join nonexistent game` → `Cannot_join_nonexistent_game`)
- Given = events, When = command, Then = events or error

## Workflow

1. **Spec** → Update `specs/mer-eller-mindre-event-model.yaml`
2. **Types** → Add/update records in Domain
3. **Decider** → Update `Evolve` and `Decide` switches
4. **Tests** → Implement GTs and GWTs from spec
5. **Web** → Update endpoints and pages

## Forbidden

- ❌ SignalR, WebSockets, SSE
- ❌ Entity Framework, Dapper, raw SQL
- ❌ Blazor Server or WASM
- ❌ Exceptions for control flow
- ❌ Mutable domain state
- ❌ `dynamic` or reflection in domain

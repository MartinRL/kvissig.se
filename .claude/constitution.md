# Constitution: Mer eller Mindre

## Core Principles

### 1. Specification First
- The `specs/game-flows.em` file is the **single source of truth**
- All behavior must be specified in emlang before implementation
- Tests are derived from `?test?` blocks, not invented

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
- No exceptions for business logic
- Use `Result<T, Error>` or similar
- Fail fast on infrastructure errors (startup, config)

### Testing
- One test class per GWT scenario group
- Test names match emlang `?TestName?`
- Given = events, When = command, Then = events or error

## Workflow

1. **Spec** → Update `specs/game-flows.em`
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

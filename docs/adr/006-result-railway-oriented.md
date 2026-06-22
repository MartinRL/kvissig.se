---
status: Accepted
type: architecture
created: 2026-06-12
revised:
---
# ADR 006: Error Handling via Result / Railway Oriented Programming

## Context
The functional core (the Decider) must report business failures — unknown
question pack, joining a started game, out-of-range difference, and so on. There
are two broad strategies:

1. Throw exceptions for business errors.
2. Return failures as values on a two-track `Result` (Railway Oriented
   Programming, ROP).

The constitution already forbids exceptions for control flow and keeps side
effects (including `throw` and `async/await`) at the edges (Functional Core,
Imperative Shell). This ADR records *how* failures are represented as values and
which C# construct carries them.

A related question is the relationship to the emlang spec. The spec
(`specs/game-flows.yaml`) uses `x:` "exception" elements (e.g. `x: GameNotFound`).
In Event Modeling an `x:` is a **business failure outcome in a swimlane**, not a
thrown exception.

## Decision

### 1. The functional core is total and synchronous
- `Decide` and `Evolve` never `throw` for business outcomes and never use
  `async`/`await`. They are pure, total functions.
- Failures are **values** on the failure track of a `Result`, following ROP. The
  happy track carries the produced `GameEvent`s; the failure track carries a
  domain `Error`.
- All I/O, time, randomness, persistence and exception handling live in the
  imperative shell (the web/host layer).

### 2. emlang `x:` maps to the Result failure track
- Each `x:` element in the spec is a domain `Error` case returned on the failure
  track — **never** a thrown C# exception.
- This is the spec→code mapping rule: a `then: [{ x: GameAlreadyStarted }]` test
  asserts `Decide` returns `Err(new GameAlreadyStarted(...))`.

### 3. Result is a native C# union type
We represent `Result<T>` with **C# 15 union types** (the `union` keyword), targeting
`net11.0` with `<LangVersion>preview</LangVersion>` until C# 15 / .NET 11 GA
(expected November 2026).

```csharp
public sealed record Ok<T>(T Value);
public sealed record Err(Error Error);
public union Result<T>(Ok<T>, Err);
```

`Decide` returns by implicit conversion from a case type; the imperative shell
consumes it with an **exhaustive switch (no default arm)**:

```csharp
public Result<IReadOnlyList<GameEvent>> Decide(GameState state, GameCommand cmd) => cmd switch
{
    OpenLobby c                    => new Ok<IReadOnlyList<GameEvent>>([new LobbyOpened(/* ... */)]),
    JoinGame  c when state.Started => new Err(new GameAlreadyStarted(c.GameId)),
    // ...
};

// imperative shell:
var response = decision switch
{
    Ok<IReadOnlyList<GameEvent>> ok => Results.Ok(ok.Value),
    Err err                         => Results.BadRequest(err.Error),
};
```

`Error` is itself a closed set of domain failures (one record per `x:` element),
so it too is a union once the case set stabilizes.

## Rationale
- **Values, not control flow**: failures compose along the railway; the type
  signature tells the whole story. No hidden `throw` paths.
- **Compiler-enforced exhaustiveness**: union switches require every case with no
  fallback arm, matching constitution §4 (Exhaustive Pattern Matching) without a
  `_ => throw` escape hatch.
- **Native over library**: native union types avoid a third-party dependency
  (e.g. OneOf) and read better than positional `T0/T1` cases. Named case types
  (`Ok<T>`, `Err`) carry intent.
- **Spec alignment**: the one-to-one `x:` ⇒ `Error` mapping keeps
  `specs/game-flows.yaml` the single source of truth and makes GWT
  `then: [{ x: ... }]` cases mechanical to implement.

## Consequences
- The domain project must move from `net9.0` to `net11.0` and set
  `<LangVersion>preview</LangVersion>` (done in the C# implementation effort, not
  the spec phase — `src/` is intentionally out of sync now).
- We take a dependency on a **preview** language feature until GA (~Nov 2026).
  `UnionAttribute`/`IUnion` may need local declaration on early previews; revisit
  when targeting a GA SDK.
- If preview risk proves unacceptable, the fallback is a hand-rolled closed record
  hierarchy (`abstract record Result<T>` with sealed `Ok`/`Err` nested records).
  The migration is nearly mechanical — the `switch` arms match on the same case
  types; only the type declaration and a temporary `_ => throw` arm differ. We
  accept the preview path now by choice (bleeding edge).
- Every business error in the spec must have a corresponding `Error` case record.

## References
- [Unions — C# feature specifications, Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions)
- [C# 15 Union Types ship in .NET 11 Preview 2](https://startdebugging.net/2026/04/csharp-15-union-types-dotnet-11-preview-2/)
- Scott Wlaschin — [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- Scott Wlaschin — Functional Core, Imperative Shell (moving IO to the edges)
- ADR 002 — Decider Pattern for Game Logic

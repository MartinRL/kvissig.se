---
status: Accepted
created: 2026-01-27
revised: 2026-05-31
---

# ADR 004: emlang YAML for Behavior Specification

## Context
We need to specify game behavior in a way that:
- Is readable by non-developers
- Maps directly to implementation
- Generates testable scenarios
- Captures the event-sourced nature

## Decision
Use **emlang YAML** (Event Modeling Language, CLI v1.0.0) as the single source of
truth for game flows. The spec lives in `specs/game-flows.yaml`.

The root is a single `slices:` map. Each slice is either:

- **direct form** — a list of step elements (no tests), or
- **extended form** — `steps:` + a `tests:` block of Given-When-Then cases.

```yaml
slices:
  JoinGame:
    steps:
      - t: Player / Join form
      - c: JoinGame
        props:
          joinCode: Guid
          playerName: string
      - e: Game / PlayerJoined
        props:
          gameId: Guid
          playerId: Guid
          playerName: string
          joinedAt: DateTimeOffset
      - v: Screen / Game lobby
    tests:
      PlayerJoins:
        given:
          - e: Game / LobbyOpened
        when:
          - c: JoinGame
            props: { playerName: Nils }
        then:
          - e: Game / PlayerJoined
            props: { playerName: Nils }
```

### Element types
Exactly one type key per element, plus an optional `props` map:

| Type      | Key  | Carries                          |
|-----------|------|----------------------------------|
| Trigger   | `t:` | actor role + originating screen  |
| Command   | `c:` | bare command name                |
| Event     | `e:` | the stream (e.g. `Game / …`)     |
| Exception | `x:` | bare error name                  |
| View      | `v:` | a view lane read model           |

Tests are `given` / `when` / `then`: `given` takes events + views, `when` takes
commands, `then` takes events + views + exceptions.

## Rationale
- **Lintable & diagrammable**: `emlang lint | parse | fmt | diagram` validate
  structure and render a visual model for review.
- **GWT tests built in**: the `tests:` block expresses Given-When-Then scenarios
  next to the slice they exercise.
- **Implementation-agnostic**: the spec describes behavior, not technology.
- **Maps 1:1 to the Decider**: each element corresponds directly to a domain type.
- **YAML-native**: standard tooling, diffs, and review apply with no custom parser.

## Mapping to Code

| emlang        | C#                                          |
|---------------|---------------------------------------------|
| `c:` command  | `record CommandName(...);`                  |
| `e:` event    | `record EventName(...);`                    |
| `x:` exception| `record ErrorName(...);` returned in `Result`|
| `v:` view     | read-model / projection                     |
| `tests:`      | xUnit test method (Given-When-Then)         |

The `Game /` prefix on events is just the **stream label**, not an aggregate — this
is the Decider pattern, not DDD. There is no "aggregate" vocabulary.

## Consequences
- The spec file is authoritative — code must match it.
- Tests are derived from the `tests:` blocks, not invented.
- Changes to behavior require a spec update first.
- Tooling (`emlang lint`) validates the spec on every change.

See `specs/CLAUDE.md` for the full authoring cheat-sheet (keys, lint rules, swimlane
and prop-type conventions).

---
status: Accepted
type: architecture
created: 2026-01-27
revised: 2026-07-09
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
truth for game flows. The spec lives in `specs/mer-eller-mindre-event-model.yaml`.

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
          - v: State / Game
            props: { phase: lobby, joinCode: joinCode, players: [hostMartin] }
        when:
          - c: JoinGame
            props: { joinCode: joinCode, playerName: Nils }
        then:
          - e: Game / PlayerJoined
            props: { playerName: Nils }
```

### Element types
Exactly one type key per element, plus an optional `props` map:

| Type      | Key  | Carries                                            |
| --------- | ---- | -------------------------------------------------- |
| Trigger   | `t:` | actor role + originating screen                    |
| Command   | `c:` | bare command name                                  |
| Event     | `e:` | the stream (e.g. `Game / …`)                       |
| Exception | `x:` | bare error name                                    |
| View      | `v:` | a view lane read model                             |
| State     | `v:` | the Decider's decision model (`v: State / Game`)   |

emlang has no dedicated state element, so **State is expressed as a view on the
`State /` lane** — it is a read model like any other, just consumed by `Decide`
instead of a human or a processor. First-class in this spec: it is the only legal
`given` of a GWT, and the folded target of the Decision Model slice's GT.

Tests are `given` / `when` / `then`: `when` takes commands, `then` takes events +
views + exceptions. emlang itself allows both events and views in `given`, but this
spec restricts it — see the test-shape rule below.

### Test shapes: GWT takes state, GT takes events

Two test shapes, mirroring the two functions of the Decider:

| Shape | Slice type | given | when | then | Verifies |
|-------|-----------|-------|------|------|----------|
| **GWT** | command/processor | `v: State / Game` only | command | events / exceptions | `Decide: (State, Command) → Result<Event[]>` |
| **GT** | view (read model) | events | — | the view | `Evolve`/fold (the eval): events → state |

**A GWT `given` never replays events — it asserts state (`v`) only.** `Decide` takes
*State*, not an event history; a GWT that starts from events would be testing fold +
decide at once, blurring which function failed. Instead, every state (`v`) used as a
GWT input has its own preceding GT (given events → then view) proving the fold
produces that state. The two shapes together correspond 1:1 with the Decider:
GT covers `Evolve`, GWT covers `Decide`, and the state `v` is the seam between them.

## Rationale
- **Lintable & diagrammable**: `emlang lint | parse | fmt | diagram` validate
  structure and render a visual model for review.
- **GWT tests built in**: the `tests:` block expresses Given-When-Then scenarios
  next to the slice they exercise.
- **Implementation-agnostic**: the spec describes behavior, not technology.
- **Maps 1:1 to the Decider**: each element corresponds directly to a domain type.
- **YAML-native**: standard tooling, diffs, and review apply with no custom parser.

## Mapping to Code

| emlang         | C#                                                        |
| -------------- | --------------------------------------------------------- |
| `c:` command   | `record CommandName(...);`                                |
| `e:` event     | `record EventName(...);`                                  |
| `x:` exception | `record ErrorName(...);` returned in `Result`             |
| `v:` view      | read-model / projection                                   |
| `tests:` GWT   | xUnit test of `Decide` (state in, events out)             |
| `tests:` GT    | xUnit test of `Evolve`/fold (events in, state (view) out) |

The `Game /` prefix on events is just the **stream label**, not an aggregate — this
is the Decider pattern, not DDD. There is no "aggregate" vocabulary.

## Consequences
- The spec file is authoritative — code must match it.
- Tests are derived from the `tests:` blocks, not invented.
- Changes to behavior require a spec update first.
- Tooling (`emlang lint`) validates the spec on every change.

See `specs/CLAUDE.md` for the full authoring cheat-sheet (keys, lint rules, swimlane
and prop-type conventions).

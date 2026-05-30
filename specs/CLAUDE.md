# Authoring the emlang spec (`game-flows.yaml`)

This folder holds the **Event-Modeling spec** for one round ("omgång") of
*Mer eller Mindre*. `game-flows.yaml` is the single source of truth that the C#
domain maps to 1:1. It is written in **emlang YAML** (CLI v1.0.0) — not the old
custom `.em` DSL.

The board this spec transcribes is `specs/MEM-omgång.png`.

## Running the tools

`emlang` lives on PATH via `~/go/bin`. If a fresh shell can't find it:

```bash
export PATH="$PATH:/c/Program Files/Go/bin:$HOME/go/bin"
```

From the repo root:

```bash
emlang lint specs/game-flows.yaml       # must print: OK (no issues found)
emlang parse specs/game-flows.yaml      # inspect parsed structure
emlang fmt  specs/game-flows.yaml -w    # format in place
emlang diagram specs/game-flows.yaml -o specs/game-flows.html   # visual review
```

`emlang` reads `.emlang.yaml` from the repo root. It ignores `slice-missing-event`
so that standalone read-model (view-only) slices stay lint-clean.

## YAML cheat-sheet

Root is a single `slices:` map. Each slice is either:

- **direct form** — a list of step elements (no tests):

  ```yaml
  slices:
    CreateGame:
      - t: GameMaster / Create game
      - c: CreateGame
      - e: Game / GameCreated
      - v: Lobby
  ```

- **extended form** — `steps:` + `tests:`:

  ```yaml
  slices:
    JoinGame:
      steps:
        - t: Player / Join game
        - c: JoinGame
        - e: Game / PlayerJoined
      tests:
        PlayerJoins:
          given:
            - e: Game / GameCreated
          when:
            - c: JoinGame
              props: { playerName: Nils }
          then:
            - e: Game / PlayerJoined
              props: { playerName: Nils }
  ```

### Element type keys (exactly one per element, plus optional `props`)

| Type      | Short | Long         |
|-----------|-------|--------------|
| Trigger   | `t:`  | `trigger:`   |
| Command   | `c:`  | `command:`   |
| Event     | `e:`  | `event:`     |
| Exception | `x:`  | `exception:` |
| View      | `v:`  | `view:`      |

### Tests (given / when / then)

- `given`: events + views only
- `when`: commands only
- `then`: events + views + exceptions only
- All sections optional.

## Swimlane / naming conventions (locked decisions)

Two user roles + System. Events live on the `Game` stream; views are bare.

- **Triggers carry the actor role**: `GameMaster / ...`, `Player / ...`, `System / ...`.
- **Commands are bare**: `CreateGame`, `SubmitGuess`.
- **Events carry the stream**: `Game / GameCreated`.
- **Exceptions and views are bare**: `GameNotFound`, `Lobby`.

Roles:

- `GameMaster /` — exactly one (Martin, id0): `CreateGame`, `StartGame`.
- `Player /` — one or more (Nils id1, Sven id2): `JoinGame`, `SubmitGuess`.
  **The Game Master also plays** — Martin guesses through the same `Player /`
  `SubmitGuess` slice (GM ⊃ Player).
- `System /` — automations/policies: score when all guesses are in, advance to the
  next question, end the game. The GM does **not** manually advance or end.

Granularity: **behavior-based slices** (one `JoinGame`, one `SubmitGuess`), not
per-player. Concrete players (Martin id0, Nils id1, Sven id2) appear as test props /
per-player test cases, keeping the model generic to N players.

## SV ↔ EN glossary

| Svenska (board)        | English (spec/code) |
|------------------------|---------------------|
| omgång (spelomgång)    | Game (session)      |
| spelledare / värd      | GameMaster (host)   |
| spelare                | Player              |
| fråga                  | Question            |
| gissning               | Guess               |
| riktning (mer/mindre)  | direction           |
| differens / skillnad   | difference          |
| poäng                  | score               |
| resultat               | Results             |
| vinnare                | winner              |

## Iterative workflow

1. **Spec first** — edit `game-flows.yaml`.
2. **Lint** after every change until `OK (no issues found)`.
3. **Diagram** and compare against `MEM-omgång.png`.
4. Then propagate to C# domain (records, Decider, tests) — a separate effort.

When a board detail is ambiguous, pick the simplest board-faithful option and leave
a `# ASSUMPTION:` comment in the YAML.

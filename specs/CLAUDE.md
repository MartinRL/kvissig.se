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
    OpenLobby:
      - t: GameMaster / Quiz catalog
      - c: OpenLobby
      - e: Game / LobbyOpened
      - v: Game lobby
  ```

- **extended form** — `steps:` + `tests:`:

  ```yaml
  slices:
    JoinGame:
      steps:
        - t: Player / Join form
        - c: JoinGame
        - e: Game / PlayerJoined
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

- **Triggers carry the actor role + originating screen**: a command is issued *from*
  a screen, which is itself a read model. Name the trigger after that screen:
  `GameMaster / Quiz catalog`, `Player / Question`, `GameMaster / Game lobby`. The
  one exception is a plain UI entry point with no backing read model — the
  `Player / Join form` (you type a join code before any game state exists for you).
  `System / ...` triggers name the policy.
- **Commands are bare**: `OpenLobby`, `SubmitGuess`.
- **Events carry the stream**: `Game / LobbyOpened`.
- **Exceptions and views are bare**: `GameNotFound`, `Game lobby`.

### Views (read models)

Resulting read models — the screens players actually see — not command echoes:

| View                  | Shown when                                  |
|-----------------------|---------------------------------------------|
| `Quiz catalog`        | GM browses packs (kviss) to start a game    |
| `Game lobby`          | After create / join, waiting to start       |
| `Question`            | A question is presented (Q0 or next)        |
| `Waiting for others`  | Player guessed, others still pending        |
| `Round results`       | Question scored, per-player round + total   |
| `Final standings`     | Game ended, scoreboard + winner             |

`Question` derives its card text/options from the chosen **question pack** by index;
those are not carried on events.

Roles:

- `GameMaster /` — exactly one (Martin, id0): `OpenLobby`, `StartGame`.
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
| kviss / frågepaket     | QuestionPack        |
| fråga (frågekort)      | Question (card)     |
| gissning               | Guess               |
| riktning (mer/mindre)  | direction           |
| differens / skillnad   | difference          |
| poäng                  | score               |
| resultat               | Round results       |
| slutställning          | Final standings     |
| vinnare                | winner              |

**QuestionPack**: a deck of designated question-cards (one card per question, e.g.
"0-100"). Choosing to play MEM = choosing a pack from the `Quiz catalog`. The
`questionPackId` on `LobbyOpened` *is* the catalog selection — there is no separate
`quizId`. The catalog is reference data, not on the `Game` stream.

## Iterative workflow

1. **Spec first** — edit `game-flows.yaml`.
2. **Lint** after every change until `OK (no issues found)`.
3. **Diagram** and compare against `MEM-omgång.png`.
4. Then propagate to C# domain (records, Decider, tests) — a separate effort.

When a board detail is ambiguous, pick the simplest board-faithful option and leave
a `# ASSUMPTION:` comment in the YAML.

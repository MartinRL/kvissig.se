# Authoring the emlang spec (`mer-eller-mindre-event-model.yaml`)

This folder holds the **Event-Modeling spec** for one round ("omgång") of
*Mer eller Mindre*. `mer-eller-mindre-event-model.yaml` is the single source of truth that the C#
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
emlang lint specs/mer-eller-mindre-event-model.yaml       # must print: OK (no issues found)
emlang parse specs/mer-eller-mindre-event-model.yaml      # inspect parsed structure
emlang fmt  specs/mer-eller-mindre-event-model.yaml -w    # format in place
emlang diagram specs/mer-eller-mindre-event-model.yaml -o specs/mer-eller-mindre-event-model.html   # visual review
```

`emlang` reads `.emlang.yaml` from the repo root. It ignores `slice-missing-event`
so that standalone read-model (view-only) slices stay lint-clean.

## YAML cheat-sheet

Root is a single `slices:` map. Each slice is either:

- **direct form** — a list of step elements (no tests):

  ```yaml
  slices:
    OpenLobby:
      - t: host / Quiz catalog
      - c: OpenLobby
      - e: Game / LobbyOpened
      - v: Roster
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
  `host / Quiz catalog`, `Player / Question`, `host / Game lobby`. The
  one exception is a plain UI entry point with no backing read model — the
  `Player / Join form` (you type a join code before any game state exists for you).
  `System / ...` triggers name the policy.
- **`⚙️ System / ...` triggers carry a gear-emoji prefix.** Rationale: in canonical
  Event Modeling a processor is drawn as a **gear icon in the initiator lane** — it sits
  where a human's UI wireframe would sit, above the command it fires (see the hotel
  example's "Checkout Processor" / "Payment Processor" gears). emlang has no cog element
  and renders a trigger as `Role → swimlane` + `Name → box`, so prefixing the System
  trigger's role (`⚙️ System`) puts the gear in the **initiator lane** — the faithful
  analog of that gear icon, NOT a slice-name marker. It parses as plain UTF-8 (lint does
  not validate name content) and renders on the `⚙️ System` swimlane label. Apply it
  CONSISTENTLY to all four System processors (`⚙️ System / Reveal direction`,
  `⚙️ System / Score difference`, `⚙️ System / Ask next question`, `⚙️ System / End game`)
  — never a subset, or the swimlane labels disagree. The System processors share ONE
  `System` initiator lane (distinguished by box name), a simplification of the canonical
  per-processor gear.
  Human-role triggers carry a head/face emoji on the SAME initiator-lane principle, one
  per swimlane so every lane is emoji-tagged (not just System): `🧑‍🏫 host /` (the
  quiz leader) and `🧑‍🎓 Player /` (the contestant). Apply each CONSISTENTLY to all of
  that role's triggers (`🧑‍🏫 host / Quiz catalog`, `🧑‍🏫 host / Game lobby`;
  `🧑‍🎓 Player / Join form`, `🧑‍🎓 Player / Question`) — same parse-as-UTF-8, renders on the
  swimlane label.
- **Commands are bare**: `OpenLobby`, `SubmitDirection`.
- **Events carry the stream**: `Game / LobbyOpened`.
- **Exceptions are bare**: `GameNotFound`.
- **Screen views are bare data nouns** (`Roster`, `Round scores`); **State/Todo views
  carry a lane**: `State / Game`, `Todo / Outstanding directions` (see below).

### Slice-name emoji prefix (slice TYPE marker)

Every slice key carries an emoji prefix so the spec reads as colour-coded columns at a
glance. Unlike `⚙️` (which marks the System *trigger*, an initiator gear), these mark
the **slice's type** — one of three buckets, applied to ALL slices (never a subset):

| Prefix | Slice type      | Meaning                                  | Slices |
|--------|-----------------|------------------------------------------|--------|
| `✍️`   | state-change    | writes the stream (command/processor)    | Open Lobby, Join Game, Start Game, Submit Direction, Submit Difference, Reveal Direction, Score Difference, Ask Next Question, End Game |
| `👀`   | state view      | reads/shows state (screens + decider State) | Quiz Catalog, Game Lobby, Question, Waiting For Others, Direction Results, Round Results, Final Standings, Decision Model |
| `📋`   | todo            | a `Todo /` read-model the gears gate on  | Outstanding Directions, Outstanding Differences, Game Progress |

`✍️` (write) / `👀` (read) are a deliberate read/write duality; `📋` is the todo subset
of read-models. A processor slice is `✍️` (it writes) AND still carries `⚙️` on its
System trigger — the two emoji are orthogonal (slice type vs initiator gear). Prefixes
parse as plain UTF-8 (lint ignores name content) and render on the slice heading.

### Automations (processor pattern)

emlang has **no dedicated cog/gear element**. An automation is the Event-Modeling
**processor pattern** (cf. the official "Invoice generation" example):

```
event(s) → Todo read-model → System trigger (the gear) → command → event(s) → Screen
```

Consequences for this spec:

- **"All directions/differences in" is a read-model condition, not an event.** It is the
  `allDirectionsIn` flag on `Todo / Outstanding directions` (gates `System / Reveal
  direction`) / the `allDifferencesIn` flag on `Todo / Outstanding differences` (gates
  `System / Score difference`). The player count is known, so there is no
  `AllGuessesSubmitted` event.
- **`DirectionScored` and `DifferenceScored` are per-player** — one event per `playerId`.
  A single `RevealDirection` emits one `QuestionDirectionRevealed` plus N `DirectionScored`;
  a single `ScoreDifference` emits one `QuestionDifferenceRevealed` plus N `DifferenceScored`.
- The four System slices (`Reveal direction`, `Score difference`, `Ask next question`,
  `End game`) each read a `Todo /` read-model before their trigger. `Ask next question` /
  `End game` both react to `QuestionDifferenceRevealed` and are mutually exclusive on
  `Todo / Game progress`'s `hasNextQuestion`.

### Views (read models)

Human screens are **bare data nouns** (named for the DATA the view carries, never the
surface that shows it — surfaces live in the xm spec); processor-facing projections
carry a lane:

- bare noun — screens players & the host actually see (`Roster`, `Round scores`).
- `Todo / ...` — read-models a `System /` processor consumes; never shown to a human.
- `State / ...` — the decider's decision model.

> **NOTE**: emlang parses + lints lane prefixes fine, but the diagram renders views by
> their bare name without a per-view lane label (only triggers and events get a
> visible swimlane label). The Todo/State prefix still documents intent and maps to code.

| View (screen, bare noun) | Shown when                                  |
|--------------------------|---------------------------------------------|
| `Pack catalog`           | host browses packs (kviss) to start a game  |
| `Roster`                 | After open-lobby / join, waiting to start   |
| `Question card`          | A question is presented (Q0 or next)        |
| `Guess progress`         | Player submitted, others still pending      |
| `Direction reveal`       | Stage 1 closed: direction revealed + bonus  |
| `Round scores`           | Question scored, per-player round + total   |
| `Scoreboard`             | Game ended, scoreboard + winner             |

| View (Todo lane)                 | Consumed by                                    |
|----------------------------------|------------------------------------------------|
| `Todo / Outstanding directions`  | `System / Reveal direction` (allDirectionsIn)  |
| `Todo / Outstanding differences` | `System / Score difference` (allDifferencesIn) |
| `Todo / Game progress`           | `System / Ask next question`, `End game`       |

`Question card` derives its card text/items from the chosen **question pack**
by index; those are not carried on events.

Roles:

- `host /` — exactly one (Martin, id0): `OpenLobby`, `StartGame`.
- `Player /` — one or more (Nils id1, Sven id2): `JoinGame`, `SubmitDirection`,
  `SubmitDifference`. **The host also plays** — Martin guesses through the same
  `Player /` slices (host ⊃ Player).
- `System /` — processors named by action: `System / Reveal direction`,
  `System / Score difference`, `System / Ask next question`, `System / End game`. The host
  does **not** manually advance or end.

### Event vocabulary

`LobbyOpened → PlayerJoined → GameStarted → DirectionSubmitted →
QuestionDirectionRevealed → DirectionScored(×players) → DifferenceSubmitted →
QuestionDifferenceRevealed → DifferenceScored(×players) → NextQuestionStarted → … →
GameEnded` — the twåstegsraket: stage 1 (direction) reveal + bonus, then stage 2
(difference) reveal + score.

- `QuestionDirectionRevealed {questionIndex, correctDirection}` — stage-1 reveal, once.
- `DirectionScored {…, playerId, guessedDirection, directionCorrect, bonusPoints}` —
  **per-player** stage-1 bonus (−10 | 0), one per player.
- `QuestionDifferenceRevealed {questionIndex, correctDifference}` — stage-2 reveal, once;
  the **progression gate** (`Ask next question` / `End game` react to it).
- `DifferenceScored {…, playerId, …, roundScore, totalScore}` — **per-player** stage-2
  score, one per player; `roundScore = differencePoints + stage-1 bonus`.
- No `AllGuessesSubmitted` event (it is a `Todo / Outstanding ...` condition).

Granularity: **behavior-based slices** (one `JoinGame`, one `SubmitDirection`), not
per-player. Concrete players (Martin id0, Nils id1, Sven id2) appear as test props /
per-player test cases, keeping the model generic to N players.

### Prop type vocabulary

emlang props are free-form annotations (`emlang lint` does not validate them), but
this spec is the single source of truth the C# domain maps to 1:1, so the annotations
**are** the intended types. Apply this vocabulary consistently:

| Prop family                                   | Type                    |
|-----------------------------------------------|-------------------------|
| `gameId` `playerId` `hostPlayerId`            | `Guid`                  |
| `questionPackId` `packId`                     | `string` (filename slug)|
| `submittedPlayerIds` `pendingPlayerIds` `winnerIds` | `Guid[]`          |
| `joinCode`                                    | `Guid`                  |
| `*At` (created/joined/started/submitted/ended)| `DateTimeOffset`        |
| `direction` `correctDirection` `guessedDirection` | `Direction (mer\|mindre)` |
| `guessedDifference` (the player's RAW absolute guess in the card's unit, `>= 0`) | `decimal` |
| `correctDifference` `guessedDifferenceNormalized` (DERIVED 0-100; never on the card) | `byte (0-100)` |
| `differencePoints` (non-negative gap `\|norm - correct\|`)        | `byte (0-100)` |
| `bonusPoints` `roundScore` `totalScore`                          | `int` (signed, may be negative) |
| `valueA` `valueB` (raw card magnitudes, any unit)                | `decimal` |
| index/count (`questionIndex`, `totalQuestions`, `questionCount`, …) | `int`  |
| `allDirectionsIn` `allDifferencesIn` `directionRevealed` `hasNextQuestion` `directionCorrect` `isHost` | `bool` |
| names/text (`hostName`, `playerName`, `questionText`, `itemA/B`, `unit`, `differencePrompt`, `name`) | `string` |

Notes:

- **`direction` is a `Direction` enum** with members `mer | mindre`. Annotated
  `Direction (mer|mindre)` on the `SubmitDirection` command **and** on the events
  (`DirectionSubmitted`, `QuestionDirectionRevealed.correctDirection`,
  `DirectionScored.guessedDirection`) — the event types are not weaker than the command.
- **The difference family is split by raw-vs-normalized (NOT all `int (0-100)`).** In
  stage 2 the player guesses the **RAW** absolute difference in the card's own unit —
  `decimal`, `>= 0`, answering the card's `differencePrompt` — carried on
  `SubmitDifference` / `DifferenceSubmitted` as `guessedDifference`. The client never sees
  the hidden values, so the **system** normalizes server-side in `ScoreDifference` with
  `mx = max(valueA, valueB)`: `correctDifference = round(|A-B|/mx*100)` and
  `guessedDifferenceNormalized = min(100, round(guessedDifference/mx*100))` (CLAMPED at
  100; same `mx` for both). Both normalized values are `byte (0-100)` and DERIVED — never
  on the card. `differencePoints = |guessedDifferenceNormalized - correctDifference|` is
  `byte`; `bonusPoints`, `roundScore`, `totalScore` are signed `int` (carry the −10 bonus,
  dealt on stage-1 `DirectionScored`). `DifferenceOutOfRange` guards a **negative** raw
  guess only — there is no upper bound (too-large clamps at 100).
- **`joinCode` is a `Guid`** — a minted join token, distinct from `gameId`.
- **Timestamps are `DateTimeOffset`** (unambiguous instant; survives serialization
  without `DateTime.Kind` loss; matches `GameEvent.OccurredAt` / `GameContext.Now`).
- Complex element types carry precise field types inline:
  `QuestionPack { packId: Guid, name: string, questionCount: int }`,
  `Player { playerId: Guid, name: string, isHost: bool }`,
  `PlayerScore { playerId: Guid, roundScore: int, totalScore: int }`,
  `ScoreboardEntry { playerId: Guid, playerName: string, totalScore: int }`.
- **`State / Game` folds the whole deck as `questions: IReadOnlyList<QuestionRound>`** —
  the game loads all questions up front and each player's stage-1/stage-2 guess folds into
  its question. `QuestionRound { card: Question, directions: IReadOnlyDictionary<Guid,
  Direction>, correctDirection: Direction?, directionScores: IReadOnlyDictionary<Guid,
  int>, differences: IReadOnlyDictionary<Guid, decimal>, correctDifference: byte?,
  roundScores: IReadOnlyDictionary<Guid, int>, scored: bool }`. Progress is DERIVED, not
  stored: `pendingDirectionPlayerIds(i)`/`allDirectionsIn(i)`, `directionRevealed(i)`,
  `pendingDifferencePlayerIds(i)`/`allDifferencesIn(i)`, `currentQuestionScored`,
  `hasNextQuestion`, and `totalScore(p) = Σ scored questions roundScores[p]`. Two
  `Todo /` read-models mirror this, one per gear: `Outstanding directions`
  (`OutstandingDirection { questionIndex, pendingPlayerIds, allDirectionsIn }`) and
  `Outstanding differences` (`OutstandingDifference { questionIndex, pendingPlayerIds,
  allDifferencesIn, directionRevealed }`).

## SV ↔ EN glossary

| Svenska (board)        | English (spec/code) |
|------------------------|---------------------|
| omgång (spelomgång)    | Game (session)      |
| värd (UI) / host (kod) | host                |
| spelare                | Player              |
| kviss / frågepaket     | QuestionPack        |
| fråga (frågekort)      | Question (card)     |
| sak (det som jämförs)  | item (itemA/itemB)  |
| enhet                  | unit                |
| gissning               | Guess               |
| riktning (mer/mindre)  | direction           |
| differens / skillnad   | difference          |
| poäng                  | score               |
| resultat               | Round scores        |
| slutställning          | Scoreboard          |
| vinnare                | winner              |

**QuestionPack**: a deck of designated question-cards (one card per question, e.g.
"0-100"). Choosing to play MEM = choosing a pack from the `Quiz catalog`. The
`questionPackId` on `LobbyOpened` *is* the catalog selection — there is no separate
`quizId`. The catalog is reference data, not on the `Game` stream.

**Question** (frågekort) compares two things (`itemA`, `itemB`), each with a hidden raw
`decimal` value (`valueA`, `valueB`) and a shared `unit`. `questionText` is a complete,
natural Swedish sentence the author writes (full control of the grammar; convention:
**Mer = `itemA` has the larger value**). `differencePrompt` is the per-card wording for
the raw-difference guess (e.g. "Hur många miljoner invånare skiljer det?") — the player
answers it in the card's unit. The card carries NO precomputed answer — `RevealDirection`
derives the direction (stage 1) and `ScoreDifference` the normalized difference (stage 2)
from the raw values at reveal.

**File-based CSV catalog**: the catalog is domain reference data stored as plain CSV files
on disk — one pack = one `*.csv`, edited by the author in Excel / Google Sheets (no DB, no
embedded resource). The packs live in the **Domain project**
(`src/MerEllerMindre.Domain/data/packs`) and are copied to output (so the Web shell and
tests find them beside the assembly). The **filename slug is the `packId`/`questionPackId`**
(`mer-eller-mindre.csv` → `mer-eller-mindre`; de-slugged → display name "Mer eller
mindre"). Files use the **Swedish Excel dialect** (`;` separator, `,` decimal, sv-SE) with
**Swedish column headers** mapped to the English domain fields via the SV↔EN glossary:

| CSV header (sv) | Domain field |
|-----------------|--------------|
| `fråga`         | `questionText` |
| `sakA` / `sakB` | `itemA` / `itemB` |
| `värdeA` / `värdeB` | `valueA` / `valueB` (decimal) |
| `enhet`         | `unit` |
| `differensfråga`| `differencePrompt` |

## Iterative workflow

1. **Spec first** — edit `mer-eller-mindre-event-model.yaml`.
2. **Lint** after every change until `OK (no issues found)`.
3. **Diagram** and compare against `MEM-omgång.png`.
4. Then propagate to C# domain (records, Decider, tests) — a separate effort.

When a board detail is ambiguous, pick the simplest board-faithful option and leave
a `# ASSUMPTION:` comment in the YAML.

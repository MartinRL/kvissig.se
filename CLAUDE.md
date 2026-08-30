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
specs/mer-eller-mindre.em.yaml   # emlang YAML spec (CLI v1.0.0) — ALL behavior defined here
specs/tasks.md                   # Implementation checklist
.claude/constitution.md          # Coding standards
```

### emlang Syntax (in mer-eller-mindre.em.yaml)

YAML element types (one key per element, plus optional `props`):

- `t:` — Trigger (actor role + originating screen)
- `c:` — Command
- `e:` — Event (carries the stream, e.g. `Game / LobbyOpened`)
- `x:` — Exception (business error)
- `v:` — View (read model)
- `tests:` — GWT cases (`given:` events/views, `when:` commands, `then:` events/views/exceptions)

See `specs/CLAUDE.md` for the full cheat-sheet.

## Constraints

**Required:**
- All public types are records
- No exceptions for business logic — use `Result<T>`
- Exhaustive pattern matching (no default/discard cases)
- Collections use `IReadOnlyList<T>`

**Forbidden:**
- SignalR, WebSockets, SSE
- Entity Framework, databases
- Blazor Server & interactive render modes (WebSocket-circuit). Static SSR with Razor
  Components (`RazorComponentResult<T>`, no `@rendermode`, no `blazor.web.js`) is the chosen
  renderer — see ADR 007.
- `dynamic` or reflection in domain

## Workflow

1. **Spec first**: Update `specs/mer-eller-mindre.em.yaml`
2. **Domain types**: Add records matching the spec
3. **Decider**: Update `Evolve` and `Decide` switches
4. **Tests**: Implement GWT from `?TestName?` blocks
5. **Web**: HTMX endpoints and Razor pages

## Game ideas / scaling

New game ideas are proven in **mini-scale** before full prod, to test the concept cheaply
(a "fråga" = one round: direction + difference):

- **Mini = 175 cards, 7-question round.** Marker: pack-slug contains `mini`. Such packs play
  `Decider.MiniGameSize` (7) and are **exempt** from the 1085-card contract test.
- **Prod = 1085 cards, 21-question round.** Everything without `mini` plays
  `Decider.FullGameSize` (21) and the `EveryFullDeckIsExactly1085Cards` test guards it.
- **Promote path:** when the concept is validated, rename the pack (drop the `mini` marker)
  + grow content to 1085 → it automatically becomes a 21-question prod deck the 1085 test
  guards.

## Copy / typografi (svensk site-copy)

- **Ingen em dash (—) i svensk copy.** Läses som osvenskt/AI-genererat. Struktura om med
  komma, parentes eller kolon. Gäller ENDAST kvissig.se site-copy, INTE artiklar utanför
  sajten, INTE kod/engelska kommentarer. En dash (–, tankstreck) och `Given–When–Then` är OK.
- **SEO-titlar med brand-suffix är OK att behålla** (t.ex. `Spel som 0-100 — prova Mer eller
  Mindre` / `... | Mer eller Mindre`) — separatorn får stå.
- **Ordet "gratis" får ALDRIG förekomma** (UI, copy, schema, docs) — kilen är twist +
  online-tillsammans, aldrig pris.

## Naming

- Commands: verb noun (`OpenLobby`, `SubmitGuess`)
- Events: noun past-tense (`LobbyOpened`, `GuessSubmitted`)
- Errors: descriptive (`GameNotFound`, `DifferenceOutOfRange`)

## Tools

`tools/` holds .NET file-based apps, run from repo root as `dotnet run tools/<name>.cs`.
Each `#:project`s the Domain so band/parsing logic has ONE source of truth (no re-implemented
formulas). No `.csproj`, no global tool.

- `tools/pack.cs` — question-pack band-histogram report (style-guide thresholds 20/60/85,
  targets 15/40/30/15) + direction split, top units, duplicate `questionText` flags.
  - `report` (default, read-only) on the live pack; `report --staging` over all
    `question-staging/*.csv` candidates.
  - `merge --out <path>` concats + dedups staging into a valid pack CSV; `--out` required,
    refuses the live-pack path unless `--force`.
- emlang codegen = the `Emlang.Generators` analyzer NuGet (same repo,
  github.com/MartinRL/xmlang): Commands/Events/Errors + Decider.g.cs + SpecTests.g.cs are
  generated into obj/ from `EmlangPrefix`-tagged AdditionalFiles (ADR 016-018, 020).
- em linter = the `Emlang.Cli` global tool (github.com/MartinRL/xmlang):
  `dotnet tool install -g Emlang.Cli`, then `em lint specs/<game>.em.yaml`
  (reads `.emlang.yaml` from repo root). Replaces the Go `emlang lint`.
- xm linter = the `Xmlang.Cli` global tool (github.com/MartinRL/xmlang):
  `dotnet tool install -g Xmlang.Cli`, then `xm lint specs/<x>.xm.yaml`.

## Plans

Plans live in `~/.claude/plans/`; the user reads/edits them in Obsidian. ALWAYS end any
plan-related message with the plan's file name (e.g. `logical-questing-teapot.md`) on the
last line so it's easy to find/open in Obsidian.

# Blindbudet — Implementation Tasks

Sister-checklist to `specs/tasks.md`. Source of truth = `specs/blindbudet.em.yaml`.
Physical progress spår for the ralph-loop (`BLINDBUDET-TASK.md`). Check off `[x]` as pieces land.

## Phase 0: Solution wiring
- [x] `Blindbudet.Domain` project created + added to the solution (net11.0, LangVersion preview via Directory.Build.props)
- [x] `Blindbudet.Domain.Tests` project created + added to the solution
- [x] `<ProjectReference>` from `Blindbudet.Domain` → `MerEllerMindre.Domain` (reuse `Result<T>` + `QuestionPackCsvParser` ONLY)
- [x] MEM projects/tests untouched (verify: MEM tests still green)

## Phase 1: Domain Types (from spec)
- [x] `AuctionPhase` enum (notCreated, lobby, started, ended)
- [x] `union AuctionCommand` (OpenAuction, JoinAuction, StartAuction, PlaceBid, RevealLot, AskNextLot, EndAuction)
- [x] `union AuctionEvent` (AuctionOpened, PlayerJoined, AuctionStarted, BidPlaced, LotRevealed, RoundScored, NextLotStarted, AuctionEnded) — each with own `*At`
- [x] `union AuctionError` (AuctionPackNotFound, AuctionNotFound, AuctionAlreadyStarted, NameAlreadyTaken, NotEnoughPlayers, BidNegative, AlreadyBid, LotAlreadyResolved, NotAllBidsIn) — parameterless markers
- [x] `Lot(Description, TrueWorth decimal, Unit)` + `AuctionPack(PackId, Name, IReadOnlyList<Lot>)`
- [x] `LotRound` (lot + bids map + trueWorth?/winnerId?/pricePaid?/profits map + resolved)
- [x] `AuctionState` (phase, joinCode, hostPlayerId, players, currentLotIndex, lots) + DERIVED members (pendingBidPlayerIds, allBidsIn, currentLotResolved, hasNextLot, totalScore)
- [x] `AuctionContext` (NewGuid/Now/FindPack, mirror MEM's GameContext form)

## Phase 2: Decider
- [x] `Evolve` — exhaustive switch, no default arm; bids fold in stream order
- [x] `Decide` — exhaustive switch; business failures via `Result<T>`, never throw
- [x] `RevealLot` resolver: highest bid wins, pays own bid, `profit = round(trueWorth − pricePaid)`, tie = earliest BidPlaced; emits LotRevealed + one RoundScored per player
- [x] `AuctionEnded` folds finalScoreboard + winnerIds (**HIGHEST total wins**; ties share)
- [x] `Fold` aggregates events into AuctionState

## Phase 3: GWT Tests (all `tests:` from the spec)
### Open / Lobby
- [x] `auction can be created`
- [x] `cannot open auction with unknown pack`
- [x] `lobby lists the host and joined players`
### Join
- [x] `player can join lobby`
- [x] `cannot join nonexistent auction`
- [x] `cannot join started auction`
- [x] `cannot join with name already taken`
### Start
- [x] `auction can be started`
- [x] `cannot start nonexistent auction`
- [x] `cannot start without enough players`
### Lot / Bidding
- [x] `current lot presented with content and progress`
- [x] `bid placed successfully`
- [x] `cannot bid in nonexistent auction`
- [x] `negative bid rejected`
- [x] `cannot bid twice on same lot`
- [x] `cannot bid on an already-resolved lot`
- [x] `shows who has bid and who is still pending`
### Outstanding bids
- [x] `every lot opens for every player when the auction starts`
- [x] `a placed bid checks off that player on its lot`
- [x] `a lot shows all bids in once every player has bid`
### Reveal & scoring
- [x] `highest bid wins and pays its bid`
- [x] `overbidding the worth yields a negative profit`
- [x] `tie on highest bid broken by earliest bid`
- [x] `scores accumulate across lots`
- [x] `cannot reveal before all bids in`
- [x] `cannot reveal an already-resolved lot`
- [x] `reveals the worth, winner and per-player profit once resolved`
### Progression
- [x] `progress shows a next lot while lots remain`
- [x] `progress shows no next lot once the last is resolved`
- [x] `next lot presented when one remains`
### End
- [x] `highest score wins`
- [x] `tied highest totals share the win`
- [x] `shows the final scoreboard and winner`
### Decision model fold
- [x] `state folds a bid into the decision model`
- [x] `resolving a lot folds the reveal and profits into that lot`

## Phase 4: Projections (pure `AuctionState → View`)
- [x] `AuctionCatalogView` (packs) — Web reference data, not a projection
- [x] `AuctionLobbyView` (gameId, joinCode, players)
- [x] `LotView` (lotIndex, totalLots, description, unit — NO trueWorth)
- [x] `WaitingForBidsView` (submitted vs pending)
- [x] `OutstandingBidsView` (one row per lot; pendingPlayerIds, allBidsIn)
- [x] `RoundResultsView` (trueWorth, winnerId, pricePaid, per-player profit + total)
- [x] `AuctionProgressView` (lotIndex, totalLots, resolvedLotCount, hasNextLot)
- [x] `FinalStandingsView` (finalScoreboard, winnerIds — folded from AuctionEnded)

## Phase 5: Architecture-contract test (NEW)
- [x] `BlindbudetArchitectureTests` parses the spec (`c:`/`e:`/`x:`) and contract-checks the auction unions (mirror MEM's `ArchitectureTests`)

## Phase 6: CSV catalog + mini-lott deck
- [x] Auction pack loaded via the existing CSV-catalog pattern (headers `beskrivning;santVärde;tema;enhet`)
- [x] `data/auction-packs/blindbudet-mini.csv` — a handful of REAL demo lots, sv-SE `;`/`,` + BOM
      (own dir, NOT `data/packs`, so MEM's 7-col QuestionPack catalog never tries to parse it)

## Phase 7: Web-shell (in `MerEllerMindre.Web`, parallel to MEM's GameEndpoints)
- [x] Blindbudet endpoints (open / join / start / bid / reveal-fires-on-allBidsIn / next / state poll)
- [x] In-memory auction event store + application service (mirror MEM's repository)
- [x] Razor screens: catalog entry, lobby (host + player), lot + bid number-field, waiting, round results, final standings
- [x] Copy makes **HÖGST vinner** glasklart; no "gratis" anywhere

## DONE gates
- [x] `dotnet build` clean, no new warnings
- [x] `dotnet test` fully green (MEM + Blindbudet, incl. `BlindbudetArchitectureTests`)
- [x] MEM tests untouched/green
- [x] `Grep -i gratis` over `src` → 0 hits
- [x] Manual: `dotnet run --project src/MerEllerMindre.Web` → played a Blindbudet round with the mini deck
      (Everest worth 8849; Martin bid 5000, Nils bid 7000 → Nils wins, pays 7000, profit 1849, total 1849 = HIGHEST wins)
- [x] Marker file `BLINDBUDET-DONE` in repo root

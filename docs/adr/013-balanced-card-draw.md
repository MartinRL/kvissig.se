---
status: Accepted
type: game-design
created: 2026-06-21
---

# ADR 013: Balanced 21-Card Draw per Game

## Context
A live pack holds ~1085–1322 cards (ADR 012), but a single game should be a tight,
varied session — not the whole pack. Each game must draw a fixed number of cards that
together span the difficulty spectrum, avoid repeating the same item twice, and be
frozen for the life of the game so replay (ADR 001) is deterministic.

This is a game-rule decision (`type: game-design`): it shapes how a session plays.

## Decision
Every game draws **exactly 21 difficulty-band-balanced, item-distinct cards** from the
chosen pack, **frozen on `LobbyOpened`**.

### N = 21, constant
```csharp
// src/MerEllerMindre.Domain/Decider.cs:17
// ponytail: fixed 21, lift to config only if a pack ever needs a different N
public const int QuestionsPerGame = 21;
```
A pack with ≤ 21 cards is used whole (`PickBalanced` returns the pool as-is).

### Algorithm: `QuestionSelection.PickBalanced` (`Decider.cs:380-492`)
1. Group the pool into the **4 bands** using the same `BandOf` / `NormalizeDifference`
   formula as ADR 012.
2. **Largest-remainder apportionment** of the 21 seats over the band target weights
   (`[15, 40, 30, 15]` → 3 / 9 / 6 / 3 of 21).
3. **Fisher–Yates shuffle** each band, then take item-distinct cards up to that band's
   quota; the rest go to a leftover pool.
4. **Fill any band deficit** from the (shuffled) leftover pool, still item-distinct.
5. **Fallback**: if the pool can't yield 21 item-distinct cards (never the live pack),
   fill the remainder allowing repeats so the game is never short.
6. **Final shuffle** of the 21 so bands don't cluster in play order.

### Item-dedup (best-effort, case-insensitive)
`TryUse` (`Decider.cs:407-413`) records each `itemA`/`itemB` in a
case-insensitive set so an over-represented item appears at most once per game. Covered
by `QuestionSelectionTests.OverRepresentedItemAppearsAtMostOnce`.

### RNG injected; core stays pure / total
```csharp
// Decider.cs:361
public record GameContext(..., Func<int, int> NextRandom)
{
    public static GameContext Default => new(..., NextRandom: Random.Shared.Next);
}
```
The draw takes RNG as an injected exclusive-upper-bound `Func<int,int>` (default
`Random.Shared.Next`; tests stub `_ => 0`). `Decide`/`Evolve` never throw and hold no
stateful RNG side effect.

### Frozen on the event, never re-drawn
`DecideOpenLobby` (`Decider.cs:139-154`) calls `PickBalanced` once and stamps the 21
cards onto `LobbyOpened.Questions` (`Events.cs`). `Evolve` replays the stored deck and
never re-selects (`specs/game-flows.yaml:147-148`). No RNG seed is persisted — it isn't
needed, because the resulting deck is already on the event.

## References
- ADR 012 (bands, quotas, the one normalization formula — not duplicated here).
- ADR 001 (event sourcing — the deck is frozen on `LobbyOpened`, replay never re-draws).
- ADR 006 (functional core total/pure → RNG as an injected `Func`).

## Consequences
- Each game is a deterministic replay of a one-time random draw — same events, same
  deck, forever.
- Changing N, the band weights, or the dedup rule is a one-line change at a stated
  decision point, not a hunt through the code.

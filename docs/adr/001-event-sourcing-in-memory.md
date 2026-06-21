---
status: Accepted
type: architecture
created: 2026-01-27
revised: 2026-06-20
---

# ADR 001: Event Sourcing In-Memory

## Context
We need to track game state (players, guesses, scores) throughout a quiz session. Options considered:
1. Traditional mutable state
2. Event sourcing with persistent store
3. Event sourcing in-memory

## Decision
Use **event sourcing with in-memory storage**.

Clients depend on the `IEventStore` interface (`Append` + `Read`, no update/delete), never on the backing store directly. The single implementation, `InMemoryEventStore`, holds each game's stream as an append-only event log: an `ImmutableArray<GameEvent>` in memory. Appends produce a new array—events are never mutated or removed. `Read` returns `IReadOnlyList<GameEvent>`, so callers never see `ImmutableArray<GameEvent>` and can't depend on the concrete storage type. Uniqueness/dedup is not a storage concern; it's enforced upstream in the Decider as business invariants. State is derived by folding events through the `Evolve` function. No persistence layer—games exist only during runtime.

## Rationale
- **Hobby project**: No need for persistence between server restarts
- **Auditability**: Can replay events to debug issues
- **Testability**: GWT tests naturally express event sequences
- **Simplicity**: No database, no serialization concerns
- **Same-room gaming**: Sessions are short-lived, players are physically together

## Consequences
- Games are lost on server restart (acceptable for same-room social gaming)
- Memory grows with events per game (bounded by short game duration)
- Clean separation between "what happened" and "what is the current state"
- Easy to add persistence later if needed

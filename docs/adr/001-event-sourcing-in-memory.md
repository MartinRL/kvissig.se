# ADR 001: Event Sourcing In-Memory

## Status
Accepted

## Context
We need to track game state (players, guesses, scores) throughout a quiz session. Options considered:
1. Traditional mutable state
2. Event sourcing with persistent store
3. Event sourcing in-memory

## Decision
Use **event sourcing with in-memory storage**.

Each game maintains a `List<GameEvent>` in memory. State is derived by folding events through the `Evolve` function. No persistence layer—games exist only during runtime.

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

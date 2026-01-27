# ADR 004: emlang for Behavior Specification

## Status
Accepted

## Context
We need to specify game behavior in a way that:
- Is readable by non-developers
- Maps directly to implementation
- Generates testable scenarios
- Captures the event-sourced nature

## Decision
Use **emlang** (Event Modeling Language) as the single source of truth for game flows.

```
/JoinGame/
  %Player:JoinGameForm%
  [JoinGame]
    joinCode
    playerName
  <Game:PlayerJoined>
    gameId
    playerId
  {Lobby}
---
?PlayerCanJoinLobby?
  <Game:GameCreated>
    gameId:game-1
    joinCode:ABC123
  [JoinGame]
    joinCode:ABC123
    playerName:Anna
  <Game:PlayerJoined>
    gameId:game-1
```

## Rationale
- **Visual clarity**: Symbols (`/` `%` `[]` `<>` `!` `{}` `?`) distinguish element types instantly
- **Event-native**: Built for event storming/modeling workflows
- **GWT tests built-in**: `?test?` blocks are Given-When-Then scenarios
- **Implementation-agnostic**: Spec doesn't prescribe technology
- **Compact**: More information density than user stories or Gherkin

## Mapping to Code

| emlang | C# |
|--------|-----|
| `[Command]` | `record CommandName(...);` |
| `<Aggregate:Event>` | `record EventName(...);` |
| `!Exception!` | `record ErrorName(...);` in Result |
| `{View}` | Projection/read model class |
| `?Test?` | xUnit test method |

## Consequences
- Spec file is authoritative—code must match
- Tests are derived from spec, not invented
- Changes to behavior require spec update first
- Tooling (linter) available for validation

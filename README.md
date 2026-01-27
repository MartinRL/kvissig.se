# Mer eller Mindre

A multiplayer quiz game where players guess percentages (0-100). Closest to the correct answer wins the round.

Optimized for **same-room social gaming** — everyone plays on their own device, including the game host.

## Quick Start

```bash
# Build
dotnet build

# Test  
dotnet test

# Run
dotnet run --project src/MerEllerMindre.Web
```

## Architecture

- **Event Sourced**: All state derived from events via the Decider pattern
- **In-Memory**: No database, games exist only during runtime
- **HTMX + Polling**: Simple real-time updates every 2 seconds
- **emlang Specification**: Behavior defined in `specs/game-flows.em`

## Project Structure

```
├── MerEllerMindre.slnx          # .NET 9 solution
├── CLAUDE.md                     # Claude Code instructions
├── specs/
│   ├── game-flows.em            # emlang specification (source of truth)
│   └── tasks.md                 # Implementation tasks
├── docs/adr/                    # Architecture Decision Records
├── src/
│   ├── MerEllerMindre.Domain/   # Game engine (pure functions)
│   └── MerEllerMindre.Web/      # HTMX web interface
└── tests/
    └── MerEllerMindre.Domain.Tests/
```

## Development with Claude Code

This project is structured for spec-driven development with Claude Code:

1. **Read the spec**: `specs/game-flows.em` defines all game behavior
2. **Check constitution**: `.claude/constitution.md` defines coding standards
3. **Follow tasks**: `specs/tasks.md` lists implementation work
4. **Run tests**: Tests are derived from emlang `?test?` blocks

### RALPH Loop Compatible

```bash
# With ralph-loop plugin installed:
/ralph-loop "Implement the next unchecked task in specs/tasks.md. 
Run tests after each change. Output <promise>TASK_COMPLETE</promise> when done."
--max-iterations 20
--completion-promise "TASK_COMPLETE"
```

## License

MIT

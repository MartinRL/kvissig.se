# ADR 005: CSV File for Questions

## Status
Accepted

## Context
Questions need to be stored and loaded. Options:
1. Database table
2. JSON file
3. CSV file
4. Hardcoded in code

## Decision
Store questions in a **CSV file**, memoized at startup into C# records.

```csv
Id,Text,Answer,Source
1,"Hur många procent av svenskarna äter surströmming årligen?",8,"SCB 2023"
2,"Hur många procent av jordens yta är täckt av vatten?",71,"NASA"
```

```csharp
public record Question(string Id, string Text, int Answer, string? Source);

// Loaded once at startup, cached in memory
public class QuestionRepository
{
    private readonly Lazy<IReadOnlyList<Question>> _questions;
    public IReadOnlyList<Question> All => _questions.Value;
}
```

## Rationale
- **Simplicity**: No database setup, no ORM
- **Editable**: Anyone can edit questions in Excel/Sheets
- **Version controlled**: Questions tracked in git
- **Fast**: One-time parse, O(1) access
- **Good enough**: ~100-500 questions fit easily in memory

## File Location
`data/questions.csv` in the web project, embedded as content.

## Consequences
- Adding questions = edit CSV + deploy
- No runtime question management UI (not needed for hobby)
- Easy to create multiple question packs (multiple CSV files)
- Validation happens at startup—fail fast on bad data

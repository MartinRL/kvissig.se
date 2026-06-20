---
status: Accepted
created: 2026-01-27
revised: 2026-06-01
---

# ADR 005: CSV File per QuestionPack

## Context
*Mer eller Mindre* asks "Är A **mer eller mindre** än B?" — every question is a
comparison between two options, not a single number. Questions are grouped into
**packs** (kviss): the spec makes a pack first-class
(`QuestionPack { packId: Guid, name, questionCount }`), the GM picks one from the
`Screen / Quiz catalog`, and events carry only a `questionIndex: int` into the
chosen pack. We need somewhere to store these cards. Options:

1. Database table
2. JSON file
3. CSV file
4. Hardcoded in code

## Decision
Store **each QuestionPack as its own CSV file**, memoized at startup into C#
records. A row is one comparison card — no single `Answer` column:

```csv
Index,Text,OptionA,OptionB,ValueA,ValueB,Source
0,"Vad är störst?","Sveriges yta","Norges yta",450295,385207,"SCB/SSB 2023"
1,"Vad är störst?","Atlanten","Indiska oceanen",106460000,70560000,"NOAA"
```

```csharp
public record QuestionCard(
    int Index, string Text, string OptionA, string OptionB,
    double ValueA, double ValueB, string? Source);

public record QuestionPack(
    Guid PackId, string Name, IReadOnlyList<QuestionCard> Questions)
{
    public int QuestionCount => Questions.Count;
}
```

`QuestionRepository` memoizes all packs at startup
(`Lazy<IReadOnlyList<QuestionPack>>`): the packs feed `Screen / Quiz catalog`, and
a single card is looked up by `(packId, index)`.

`correctDirection (mer|mindre)` and `correctDifference (0-100)` are **derived
read-side** from `ValueA` vs `ValueB` (difference normalized 0-100) — never stored
in the CSV. This is consistent with the spec's rule that events carry only the
`questionIndex`.

> `packId: Guid` is assigned per pack — e.g. a stable Guid per file, or a tiny
> `packs` manifest mapping CSV file → `packId` + `name`. The exact minting
> mechanism is left open; only the identity (`Guid`) is fixed.

## Rationale
- **Simplicity**: No database setup, no ORM.
- **Editable**: Anyone can edit a pack in Excel/Sheets.
- **Version controlled**: Packs tracked in git.
- **Fast**: One-time parse, O(1) lookup by `(packId, index)`.
- **Good enough**: A handful of packs of ~100-500 cards fit easily in memory.
- **Matches the spec 1:1**: mirrors `QuestionPack` + `optionA`/`optionB`; the
  in-pack `Index` *is* the `questionIndex` the events carry.

## File Location
`data/<pack>.csv` — one CSV per pack in the web project, embedded as content
(instead of one global `questions.csv`).

## Consequences
- Adding a pack = drop a new CSV file (+ a manifest/Guid entry) and deploy.
- `Index` is the in-pack identity; events reference cards by `(packId, index)`.
- `correctDirection` / `correctDifference` are derived from `ValueA`/`ValueB`, not
  stored — a single source for the answer, no risk of drift.
- No runtime question-management UI (not needed for a hobby project).
- Validation happens at startup — fail fast on a malformed pack.

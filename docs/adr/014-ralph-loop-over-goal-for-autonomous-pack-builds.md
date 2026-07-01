---
status: Accepted
type: agentic-engineering
created: 2026-06-29
---
# ADR 014: Ralph Loop over Built-in /goal for Autonomous Pack Builds

## Context
Autonomt kortleksbygge (mini 175 / prod 1085 kort) körs via en flerstegs-pipeline —
`fragesattare → faktagranskare → sprakgranskare → tydlighetsgranskare → report → commit` —
styrd av en task-fil (`RALPH-TASK.md`). Bygget är ett maraton: prod-decken kräver ~40
batchar och dokumenterade fällor (theme-drift → cap-blowup; se `familj-ralph-loop.md`).

Frågan: byta till inbyggda `/goal` istället för den installerade ralph-loop-pluginen?
De två är inte utbytbara — de löser olika problem.

## Decision
**Behåll ralph-loop** som drivare för det autonoma pack-bygget. `/goal` är INTE en
ersättare; enbart `/goal` vore en regression på drift-kontroll.

## Rationale (bevekelsen)

| | ralph-loop | /goal |
|---|---|---|
| Driv-mekanism | stop-hook återinjicerar HELA prompten verbatim varje varv | stop-hook + Haiku-utvärderare läser transkriptet |
| Drift-kontroll | instruktioner + avoid-lista friskas upp varje varv (avgörande över 40 batchar) | ingen arbetsprompt återinjiceras → drift vid kompaktering |
| Verifiering | modell self-reportar promise-sträng | utvärderare kan EJ köra kommando / läsa fil |
| Villkor | exakt strängmatch (SUCCESS vs BLOCKED skiljs) | naturligt språk ≤4000 tecken, "stop after N turns" |
| Setup | plugin-hook, Windows git-bash-skörhet (README-workaround) | inbyggt, CC ≥ 2.1.139 |

- Bärande skäl: **verbatim-återinjektionen** håller fragesattaren i schack — pipeline-
  instruktioner och avoid-lista friskas upp efter varje kompaktering. `/goal` saknar det:
  den lilla utvärderar-modellen dömer ett villkor mot transkriptet men återinjicerar ingen
  arbetsprompt, så agenten tappar pipeline-detaljerna vid kompaktering och driftar.
- Hård grind ligger oavsett i fil-marker (`CARDS-DONE` / `FAMILJ-DONE`) + `dotnet test` +
  människa. `/goal` köper ingen extern verifiering — dess utvärderare kan varken köra
  `dotnet test` eller läsa filer.

## /goal-styrkor (noterade)
- Inbyggt → ingen Windows-hook-skörhet, renare anrop + auto-clear.
- Kan uttrycka villkor som "klar ELLER skriv RALPH-BLOCKED".
- Bra för korta, transkript-verifierbara uppgifter — inte för långa drift-känsliga maraton.

## References
- ralph-loop README (plugin).
- `/goal`-doc (code.claude.com/docs/en/goal).
- `RALPH-TASK.md` (task-filen som återinjiceras).
- ADR 005 (CSV-frågor), ADR 012 (banded deck), ADR 013 (21-card draw) — pack-modellen.

## Consequences
- Pack-bygget fortsätter på ralph + `RALPH-TASK.md`.
- Windows-workarounden i README kvarstår.
- `/goal` kan läggas till som komplement (ev. grind) senare utan att riva ralph.

# Ralph-prompt-mall — Mer eller Mindre

Återanvändbar mall för `/ralph-loop`-körningar. Kopiera blocket nedan, fyll i
`{{MÅL}}`, och kör. Den tvingar fram spec-first-disciplinen och använder grön
test-svit som DONE-villkor så loopen inte itererar vidare på trasig kod.

## Så kör du

```
/ralph-loop "$(cat .claude/ralph-prompt.md)" --max-iterations 12 --completion-promise "RALPH-DONE"
```

Eller klistra in det fyllda PROMPT-blocket direkt. Justera `--max-iterations`
defensivt per uppgift (en loop som snurrar på samma problem bränner körningar).

---

## PROMPT (kopiera och fyll i {{MÅL}})

Du arbetar iterativt på **Mer eller Mindre** (C#, event-sourced Decider-pattern).
Läs `CLAUDE.md`, `.claude/constitution.md` och `specs/CLAUDE.md` om du inte redan
har dem i kontext. Följ dem exakt.

### Mål för denna körning
{{MÅL}}

### Obligatorisk arbetsordning (spec-first — hoppa ALDRIG över steg 1)
1. **Spec** — uppdatera `specs/mer-eller-mindre-event-model.yaml` (emlang YAML) FÖRST. Lägg till/justera
   `t/c/e/x/v` och `tests:` (given/when/then). Inga `tests:` får hittas på i koden —
   de härleds härifrån. Linta specen om emlang CLI finns på PATH.
2. **Domän** — lägg till/uppdatera records i `MerEllerMindre.Domain` så de matchar
   specen. Alla publika typer är records, kollektioner `IReadOnlyList<T>`.
3. **Decider** — uppdatera `Evolve` och `Decide` med exhaustiva switch-uttryck.
   Inga default/discard-cases. Business-fel via `Result<T>`, aldrig exceptions.
4. **Tester** — implementera GWT-fallen från specens `tests:`. Testnamn = emlang-namn.
5. **Web** — endast om målet kräver det: HTMX-endpoints + Razor. Ingen SignalR/
   WebSockets/SSE/EF/Blazor.

### Varje iteration
- Läs vad föregående varv lämnade (filer + git-historik bevaras mellan varv).
- Gör nästa minsta meningsfulla steg mot målet. Överarbeta inte.
- Kör `dotnet build` och `dotnet test`. Om något fallerar: fixa rotorsaken, fortsätt
  loopen. Bypassa ALDRIG (`--no-verify`, skippade tester etc.).
- `ArchitectureTests` (fitness functions) röda = arkitekturbrott i din kod. Fixa koden —
  försvaga ALDRIG testerna.
- Bocka av relevant punkt i `specs/tasks.md` när den är klar.

### DONE-villkor (skriv exakt `RALPH-DONE` först när ALLT nedan stämmer)
- `dotnet build` lyckas utan varningar relaterade till ändringen.
- `dotnet test` är helt grön.
- Specen, domänen, decidern och testerna är konsistenta med varandra.
- Målet ovan är uppfyllt och berörda punkter i `specs/tasks.md` är avbockade.

Om du fastnar på samma problem två varv i rad: stanna, beskriv blockeringen kort,
och skriv `RALPH-BLOCKED` istället för att fortsätta snurra.

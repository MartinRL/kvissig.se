---
status: Accepted
type: game-design
created: 2026-07-04
---

# ADR 015: Entydigt verifierbart santVärde per lott

## Context
BlindBudet har inget riktningsval (till skillnad från Mer eller Mindre, där spelaren svarar
[Mer]/[Mindre]) — spelaren budar mot ETT dolt `santVärde`. Flera lott i den tidiga
`blindbudet-mini.csv` namngav en **generisk kategori** ("Toppfarten för en sportbil",
"Vikten av en personbil", "ett propellerplan") som har ett *spann* av sanna värden, inte ett
enda verifierbart facit. En Bugatti gör 490 km/h, en Volvo 180 — "300 för en sportbil" är
ogissbart och godtyckligt. Spelet blir varken rättvist eller kul.

Detta är en game-rule-decision (`type: game-design`): den formar vad ett kort får vara.

## Decision
Varje lott måste ha **ETT entydigt, oberoende verifierbart `santVärde`** — ett FAKTUM med ett
enda korrekt svar, inte det "typiska" värdet för en luddig kategori.

### OK-källor till ett enda sant värde
1. **Namngiven unik entitet** — Everest, Eiffeltornet, Jupiter; en specifik bil-/MC-/
   cykelmodell.
2. **Dokumenterad art-kanon** — etablerad artsiffra (gepardens toppfart, blåvalens vikt,
   människokroppens ben/tänder/puls).
3. **Standard/regel-mått** — regel-vikt på sportbollar, olympisk bassäng 50 m, schackpjäser
   32, EU-äggstorlek M.
4. **Lag/reglering (SE/EU)** — t.ex. farten där elcykel-assistansen stryps (25 km/h, EU).

### Förkastas
Generisk tillverkad/luddig kategori utan kanonisk medlem: "en sportbil", "en personbil",
"ett propellerplan", "en vattenmelon".

### Kontrast mot Mer eller Mindre
MEM:s `tydlighetsgranskare` vaktar **riktningens** entydighet (går [Mer]/[Mindre] att svara
på utan dold översättning). BlindBudet har ingen riktning — här vaktas i stället
`santVärde`ts entydighet: skulle två pålästa personer läsa upp SAMMA facit?

## Enforcement
- Agenten `entydighetsgranskare` (`.claude/agents/entydighetsgranskare.md`) kör read-only på
  `data/auction-packs/*.csv` och flaggar lott vars `santVärde` inte är ett enda entydigt
  faktum. Motsvarar MEM:s `tydlighetsgranskare`, riktad på `santVärde` i stället för riktning.
- Kurering: flaggade kort går tillbaka till författare/faktagranskning innan leken behålls.

## Consequences
- **Referentbyte ⇒ omfaktagranskning.** Byte av sak (t.ex. "sportbil" → "Bugatti Chiron")
  ÄNDRAR sanningen → det nya `santVärde` måste web-verifieras per omskrivet kort.
- Leken förblir rättvis och gissbar: varje bud går att förlora eller vinna mot ett faktum,
  inte mot en gissning om vilken medlem av en luddig kategori författaren tänkte på.

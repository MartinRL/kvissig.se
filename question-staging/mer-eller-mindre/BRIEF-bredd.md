# BRIEF-bredd: mer-eller-mindre (baspacket) — musik (2 batchar à ~27)

Pack-profil: **vuxen allmänbildning**. Bandmål **15/40/30/15**. Mer = sakA större,
inga temporala riktningskort (årtal + före/efter), pris-ord förbjudna, sv-SE CSV.
INGEN geografi-batch här — packet har redan ~30 % geografi.

## Baseline (live-pack 2026-08-23, refresha med rapport före författande)
- Band 15,9 / 42,3 / 26,9 / 14,8 — direction Mer 53,6 % (nya batchar får gärna luta Mindre).
- **Items over cap 4 (67 st!, ABSOLUT AVOID — topp):** Eiffeltornet (21), Människa (13),
  Big Ben (11), Cheopspyramiden (11), Globen (11), Lutande tornet i Pisa (10), Pluto (10),
  Vänern (10), Burj Khalifa, Empire State Building, Jorden, Jupiter, Månen, Tokyo Skytree,
  Öresundsbron, Avokado, Europa, Flodhäst, Gepard, Kilimanjaro, Maraton, Neptunus, Triton,
  Vättern, Övre sjön ... — kör rapporten och läs HELA 67-listan innan författande.
- Swap-urvalet ska i första hand stryka kort vars items ligger över cap.

## Kategori: musik (~55 kort, 2 batchar)
Musik = musikens ENTITETER: instrument, artister/grupper, låtar/album, verk, tävlingar.
INTE byggnader (operahus = byggnad), INTE artistfödelseår-som-årtal.

- **Ribba vuxen allmänbildning:** hela registret OK — Beatles-diskografi, Mozart-symfonier,
  Eurovision-historik, jazz, svensk musikexport (ABBA, Roxette, Avicii, Max Martin),
  instrumentfakta, operaverk.
- **Stabila mått only:** låt-/albumlängd (kanonisk studioversion), antal medlemmar,
  sträng-/tangentantal, instrumentvikt/-längd, antal studioalbum/#1-hits (årspinnat om
  karriären ej avslutad), Eurovision-/Melodifestivalen-poäng (historiskt fixt),
  ålder-vid-händelse (EJ årtal), publikrekord (namngiven konsert), antal satser/akter,
  speltid för namngivna verk.
- **Förbjudet:** streams/månadslyssnare (volatilt; om nödvändigt: årspinna i frågetexten),
  "en typisk låt" (spann, ej faktum), temporala före/efter-kort med årtal.

## Format
Batch-CSV sv-SE (`;`, `,`-decimal, UTF-8 BOM), header
`fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`.
Sidecar `<batch>.källor.csv` med header `fråga;källa;år`, POSITIONELL join (rad N ↔ rad N).
Filnamn: `musik-01.csv`, `musik-02.csv` + sidecars.
Riktning ~50/50 FRÅN START (gärna svag Mindre-övervikt, se baseline).
Förvanska ALDRIG siffror för att träffa band — byt par.

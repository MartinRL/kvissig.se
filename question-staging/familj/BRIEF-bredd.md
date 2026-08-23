# BRIEF-bredd: familj — musik + geografi (2 + 2 batchar à ~27)

Bas-briefen `BRIEF.md` gäller FULLT UT (igenkänningsribba 6+, enheter, promptklarhet,
bandmål **5/25/40/30**, Mer = sakA större, inga temporala riktningskort, pris-ord förbjudna).
Detta är kategori-tillägget.

## Baseline (live-pack 2026-08-23, refresha med rapport före författande)
- Band 5,5 / 27,7 / 37,8 / 28,9 — direction Mer 51,0 %.
- **Items over cap 4 (16 st, ABSOLUT AVOID):** Sydney Operahus, Eiffeltornet, London Eye,
  Empire State Building, Notre-Dame, Sagrada Familia, Atomium, Chrysler Building,
  Frihetsgudinnan, Lutande tornet i Pisa, Petronas Towers, Stonehenge, Storkyrkan,
  Triumfbågen i Paris, Washington Monument, Willis Tower.
- **At cap (4, får EJ användas igen):** Afrikansk elefant, Basketboll, Bergen, Bi, Big Ben,
  Björn, Blåval, Bowlingklot, Brachiosaurus m.fl. — kör rapporten och läs hela listan.

## Kategori: musik (~55 kort, 2 batchar)
Musik = musikens ENTITETER: instrument, låtar, artister/grupper, körer/orkestrar.
INTE byggnader (konserthus = byggnad, redan kapat), INTE artistfödelseår-som-årtal.

- **Ribba 6+:** instrument (piano, gitarr, fiol, trummor, blockflöjt), barnvisor,
  artister barn känner (ABBA-nivå), Melodifestivalen-klassiker.
- **Stabila mått only:** antal strängar/tangenter/ventiler, instrumentvikt/-längd,
  antal medlemmar i grupp, låtlängd (kanonisk studioversion), antal album (årspinnat i
  frågetexten om det behövs), ålder-vid-händelse (EJ årtal).
- **Förbjudet:** streams/månadslyssnare, "en typisk låt" (spann, ej faktum),
  temporala före/efter-kort med årtal.
- Exempel-typ: "Har ett piano fler eller färre tangenter än en gitarr har strängar?"

## Kategori: geografi (~55 kort, 2 batchar)
Länder, floder, berg, sjöar, öar på HÖG igenkänningsnivå (barn + mormor känner båda).

- **OBS:** landmärken/byggnader räknas INTE som geografi här — de är redan över cap.
  Håll dig till naturgeografi + länder.
- Mått: höjd (meter), längd (km), yta, djup, antal (öar, länder som gränsar), °C.
- Undvik folkmängd (BRIEF.md: nej huvudstadsfolkmängd) och länder-efter-yta-nischen; men
  berg vs berg, flod vs flod, sjö vs sjö med tydliga glapp är kärnan.

## Format
Batch-CSV sv-SE (`;`, `,`-decimal, UTF-8 BOM), header
`fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`.
Sidecar `<batch>.källor.csv` med header `fråga;källa;år`, POSITIONELL join (rad N ↔ rad N).
Filnamn: `musik-01.csv`, `musik-02.csv`, `geografi-01.csv`, `geografi-02.csv` + sidecars.
Riktning ~50/50 FRÅN START. Förvanska ALDRIG siffror för att träffa band — byt par.

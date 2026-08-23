# BRIEF-bredd: alla-aldrar — musik + geografi (2 + 2 batchar à ~27)

Pack-profil: brett igenkänt **13-83 år**. Bandmål **10/35/35/20**. Mer = sakA större,
inga temporala riktningskort (årtal + före/efter), pris-ord förbjudna, sv-SE CSV.

## Baseline (live-pack 2026-08-23, refresha med rapport före författande)
- Band 9,7 / 34,9 / 35,3 / 20,1 — direction Mer 50,4 %. 0 items over cap.
- **At cap (4, får EJ användas igen):** ABB, Afrikansk elefant, Angelfallen, Angkor Wat,
  Atomium, Baikalsjön, Banan, Big Bens torn, Blåval, Brasilien, Burj Khalifa, Bäver,
  Cheddarost, Cheopspyramiden, Christ the Redeemer, CN-tornet, Coca-Cola, Colosseum,
  Concorde, Cykel, Donau, Egypten, Eiffeltornet, Ekorre, Empire State Building m.fl.
  — kör rapporten och läs HELA at-cap-listan (många fler på 4).

## Kategori: musik (~55 kort, 2 batchar)
Musik = musikens ENTITETER: instrument, artister/grupper, låtar/album, festivaler/tävlingar.
INTE byggnader, INTE artistfödelseår-som-årtal.

- **Ribba 13-83:** ABBA, Beatles, Elvis, Avicii, Roxette, Melodifestivalen/Eurovision,
  klassiska instrument, välkända symfoniorkestrar. Både en 13-åring och en 83-åring ska
  känna igen BÅDA sakerna.
- **Stabila mått only:** låt-/albumlängd (kanonisk studioversion), antal medlemmar,
  sträng-/tangentantal, instrumentvikt/-längd, antal studioalbum (årspinnat om ej avslutad
  karriär), antal #1-hits (årspinnat), Eurovision-/Melodifestivalen-poäng (historiskt fixt),
  ålder-vid-händelse (EJ årtal), publikrekord (namngiven konsert).
- **Förbjudet:** streams/månadslyssnare (volatilt), "en typisk låt" (spann),
  temporala före/efter-kort med årtal.

## Kategori: geografi (~55 kort, 2 batchar)
Länder, floder, berg, sjöar, öar, öknar på hög igenkänningsnivå.

- Naturgeografi framför byggnader/landmärken (många landmärken redan at cap).
- Mått: höjd, längd, yta, djup, medeltemperatur, antal grannländer/öar, folkmängd OK här
  (pack-standard "miljoner invånare" finns redan) men årspinna i frågetexten vid behov.
- Tydliga glapp framför nära-lika riktningsfällor (bandmål 10/35/35/20).

## Format
Batch-CSV sv-SE (`;`, `,`-decimal, UTF-8 BOM), header
`fråga;sakA;sakB;värdeA;värdeB;enhet;differensfråga`.
Sidecar `<batch>.källor.csv` med header `fråga;källa;år`, POSITIONELL join (rad N ↔ rad N).
Filnamn: `musik-01.csv`, `musik-02.csv`, `geografi-01.csv`, `geografi-02.csv` + sidecars.
Riktning ~50/50 FRÅN START. Förvanska ALDRIG siffror för att träffa band — byt par.

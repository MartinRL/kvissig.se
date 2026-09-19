---

kanban-plugin: board

---

## Planerat

- [ ] Feedback-kanal: spelet saknar kontaktmöjlighet helt. Minsta version = mailto/formulär på slutskärmen (Final standings) för alla tre spelen
- [ ] Loading states: `hx-indicator`/`.htmx-request`-CSS på gissa/bjud/lös-knapparna så dubbelklick och "hände nåt?" försvinner
- [ ] Dela-CTA på slutskärmen (ShareButton finns bara i katalogerna) + Plausible-event på share-klick, så PLG-frågan "delar spelarna?" går att mäta
- [ ] Coverage-metrik: coverlet + CI-tröskel, billigaste kvarvarande ACMM-signalen (docs/analysis/acmm.md §Upgrade path)
- [ ] ACMM L4 Adaptive: andra slutna loopen, `tools/pack.cs` band-trösklar auto-rejectar kandidater i stället för att rapportera (acmm.md §L4)
- [ ] SEO content-backlog: per-pack-/temasidor + intern länkning mellan content-sidorna (specs/seo-geo.md §6) + ev. /mattespel-landningssida
- [ ] GSC-uppföljning: följ "spel som 0-100" / "alternativ till 0-100"-queries, utvärdera om 0-100-vinkeln drar trafik (seo-geo.md §7)
- [ ] Lean MVP: knapp "köp detta spel som app till en engångskostnad precis som en fysisk kortlek", mät klick i Plausible innan nåt byggs
- [ ] Logisk nästa lokala marknad (ej blott språk, utan land/kultur: t.ex. Sverige OCH svenska)
- [ ] emlang 0.5.0 följdarbete: SpecModel/TestModel-alias `err`/`exception` → `rej`/`rejection`, CLAUDE.md + specs/CLAUDE.md (`t:` = Translator, `x:` = Rejection); när nuget.org har 0.5.0/0.6.1: ta bort local-nuget/ + källraden i NuGet.config


## Igång
- [ ] xmlang-repot: rätta 7 stale Emlang.Tests så release-workflows blir gröna (blockerar riktig publicering av emlang-v0.5.0 / xmlang-v0.6.1)


## Färdigt

- [x] Xmlang 0.6.0 → 0.6.1 + Emlang 0.5.0 via local-nuget/ (packat ur taggat xmlang-källträd, Docker-restore-lagret kopierar mappen); em 0.5.0 / xm 0.6.1 som globala verktyg; 315 tester gröna, em lint OK
- [x] Mobile-friendly CSS: mobile-first `.wrap` 560px, viewport meta, telefon-bugfixar i bugs.md, PWA narrow-screenshot
- [x] Error handling UI: `htmx:responseError` → `#error-banner` i MainLayout
- [x] ACMM full Level 3 (measured, CodeHealth-gate = första slutna loopen; verdict 2026-07-02 i docs/analysis/acmm.md)
- [x] Create ticket board (BOARD.md, Obsidian Kanban); open items moved here from specs/tasks.md + seo-geo.md
- [x] xmlang v0.6 migration of all three xm specs (confirm and then judgments)
- [x] Adopt the emlang decider dialect + bring Emlang.Generators in-repo (src/Emlang.Generators; ADR 016-018)
- [x] Rename specs to `<game>.em.yaml`, guarded by MEM ArchitectureTests
- [x] Emlang extraction to github.com/MartinRL/xmlang: NuGet Emlang + Emlang.Generators + Emlang.Cli (`em lint`), kvissig cutover (ADR 020)
- [x] xm runtime interpreter live for all 3 games (Xmlang NuGet, XmCatalog fail-fast, closed Field vocabulary)
- [x] Värden väljer rundantal 4-21 (MEM + Tänk Till Tusen, RoundCountOutOfRange)
- [x] Tänk Till Tusen (spel #3): spec → Decider → web shell, difficulty familj/klassisk/svår (specs/tank-till-tusen-tasks.md)
- [x] BlindBudet (spel #2): full stack from spec, 175-lot mini pool, ADR 015 entydigt santVärde (specs/blindbudet-tasks.md)
- [x] Mini decks à 175 kort: hundraser, elbil, fotboll, loggor (med källa per kort)
- [x] Prod decks à 1085 kort: familj + alla-aldrar via fragesattare→faktagranskare→sprakgranskare→tydlighetsgranskare
- [x] Tvåstegsraket: riktning först (-10), sedan slider i 0-100-ramen; inga råvärden på gisskärmen (tasks.md Phase 11)
- [x] Analytics: Plausible Cloud (traffic + server-side gameplay funnel)
- [x] SEO/GEO: /spel-som-0-100, sitemap, llms.txt, Google Search Console (specs/seo-geo.md)
- [x] CodeHealth gate CH ≥ 9.4 (Stop hook + CI)
- [x] Deploy: fly.io from GitHub Actions, kvissig.se + www via Cloudflare DNS, certs (tasks.md Phase 10)
- [x] PWA manifest + screenshots, favicon, % on slider, round # in scoreboard (tasks.md Phase 9)
- [x] Mer eller Mindre core: domain, Decider, GWT, CSV catalog, repository, projections, endpoints, Razor + HTMX (tasks.md Phase 1-8)
- [x] specs/bugs.md: 20 bugs logged, all fixed


## Arkiv

- [ ] Feature-flagging för WIP-kortlekar: täcks av mini-pack-konventionen (175 kort, `mini` i slug = konceptskala), återuppliva om en deck behöver döljas helt
- [ ] ACMM level 5/6: explicit icke-mål (acmm.md: "solo weekend project, not a 24/7 CNCF dashboard")




%% kanban:settings
```
{"kanban-plugin":"board"}
```
%%

---

kanban-plugin: board

---

## Backlog

- [ ] Feedback: spelet saknar kontaktmöjlighet helt (skicka feedback efter avslutat spel?)
- [ ] Mobile-friendly CSS
- [ ] Error handling UI
- [ ] Loading states
- [ ] PLG: hur får vi spelare att dela spelet med familj och vänner?
- [ ] Logisk nästa lokala marknad (ej blott språk, utan land/kultur: t.ex. Sverige OCH svenska)
- [ ] Lean MVP: knapp "köp detta spel som app till en engångskostnad precis som en fysisk kortlek"
- [ ] Feature-flagging, t.ex. WIP-kortlekar
- [ ] SEO mattespel (specs/seo-geo.md)
- [ ] GSC: följ "spel som 0-100" / "alternativ till 0-100"-queries, utvärdera om 0-100-vinkeln drar trafik (seo-geo.md §7)
- [ ] ACMM level 4
- [ ] ACMM level 5
- [ ] ACMM level 6


## Doing

- [ ] ACMM level 3 (half-way)


## Done

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
- [x] ACMM level 1 + 2
- [x] Deploy: fly.io from GitHub Actions, kvissig.se + www via Cloudflare DNS, certs (tasks.md Phase 10)
- [x] PWA manifest + screenshots, favicon, % on slider, round # in scoreboard (tasks.md Phase 9)
- [x] Mer eller Mindre core: domain, Decider, GWT, CSV catalog, repository, projections, endpoints, Razor + HTMX (tasks.md Phase 1-8)
- [x] specs/bugs.md: 20 bugs logged, all fixed


## Archive





%% kanban:settings
```
{"kanban-plugin":"board"}
```
%%

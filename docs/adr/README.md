# Decision Records

One numbered log holds both **architecture** decisions (ADR — structure, quality attributes,
dependencies, construction technique) and **operations** decisions (ODR — vendor/runtime
choices that don't shape the system's structure). They share a single numbering and the
`adr/` folder (kept deliberately, to avoid path churn); the `type:` frontmatter field tells
them apart. The taxonomy now carries **three** `type` values — `architecture`, `operations`,
and **`game-design`** (product / game-rule decisions: how the game plays, not how it's built).

| ADR | Title | Type | Status |
|-----|-------|------|--------|
| [001](001-event-sourcing-in-memory.md) | Event Sourcing In-Memory | architecture | Accepted |
| [002](002-decider-pattern.md) | Decider Pattern for Game Logic | architecture | Accepted |
| [003](003-htmx-polling.md) | HTMX with Polling for Real-Time | architecture | Accepted |
| [004](004-emlang-specification.md) | emlang for Behavior Specification | architecture | Accepted |
| [005](005-csv-questions.md) | CSV File for Questions | architecture | Accepted |
| [006](006-result-railway-oriented.md) | Error Handling via Result / Railway Oriented Programming | architecture | Accepted |
| [007](007-razor-components-static-ssr.md) | Razor Components in Static SSR as the Renderer | architecture | Accepted |
| [008](008-plausible-analytics.md) | Plausible Cloud for Analytics | operations | Accepted |
| [009](009-fly-io-hosting.md) | fly.io for Hosting | operations | Accepted |
| [010](010-github-repository.md) | GitHub for Repository Hosting | operations | Accepted |
| [011](011-github-actions-ci.md) | GitHub Actions for CI/CD | operations | Accepted |
| [012](012-difficulty-banded-deck.md) | Difficulty-Banded Deck Balancing | game-design | Accepted |
| [013](013-balanced-card-draw.md) | Balanced 21-Card Draw per Game | game-design | Accepted |
| [014](014-ralph-loop-over-goal.md) | Ralph Loop over Built-in /goal for Autonomous Pack Builds | operations | Accepted |
| [015](015-entydigt-santvarde.md) | Entydigt verifierbart santVärde per lott | game-design | Accepted |
| [016](016-generated-stratum-1-records.md) | Stratum-1 Records Are Build Artifacts Generated from the emlang Spec | architecture | Accepted |
| [017](017-generated-spec-tests.md) | Spec `tests:` Sections Are Compiled into xUnit Facts at Build Time | architecture | Accepted |
| [018](018-generated-decider-skeletons.md) | Decider Evolve/Decide Switch Skeletons Are Generated (CS8795 Seam) | architecture | Accepted |

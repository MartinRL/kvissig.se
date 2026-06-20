# Practise What You Preach

> A 47-year-old cost model prices my weekend at $492,788.

## The Anchor

A 47-year-old cost model prices my weekend at **$492,788**.

I ran `scc` — a tool that counts lines of code — on a multi-player quiz game I built solo over a
weekend. It spat out the COCOMO estimate below. Cost circled in magenta, just to be safe:

![[assets/cocomo.png]]

Nearly half a million dollars. **10.51 months. 4.17 people.** For one weekend, alone.

Before we go any further: notice what that number just did to you. You now have a reference
point. Everything I say next, you'll unconsciously measure against half a million dollars.
That's no accident — it's an **anchor** (Kahneman & Tversky: we cling to the first number we
see, however arbitrary it is). I name it on purpose, so I'm in on the joke and not its victim.
The number is wrong. But look at what it does to your expectations.

And before some engineer in the back seat drops their own counter-anchor — *"pfft, a
vibe-coded weekend toy, worth maybe $0"* — no. That's just as wrong, in the other direction.
This piece is about what lies in between, and why the distance isn't measured in lines of code.

## The Anchor Lies in Both Directions

COCOMO (Constructive Cost Model, Barry Boehm, **1981**) estimates cost from a single input:
the number of lines of code, run through a rate calibrated against waterfall projects on
mainframes. It lies in two ways at once.

**It inflates.** Of the 18,988 lines `scc` counted, masses of it isn't hand-written
application logic: 5,306 lines of CSV question data, 4,542 lines of Markdown (specs, ADRs,
documents of this very kind), generated and configured code. And even the genuine code is
priced at 1981 rates — an era without frameworks, package managers, or a standard library
that does the heavy lifting.

Scale down to **clean code only** — C#, Razor, CSS — and the model says:

> **$126,384 · 6.27 months · 1.79 people** (4,347 lines of code)

More honest. Still absurd for a solo weekend.

**It's blind.** What COCOMO *can't* see is where all the work actually lives: the
specification that is the source of truth, the seven architecture decisions, the test design,
the agent pipeline that reviews the question data. And not just the invisible *thinking* —
also the concrete work that isn't application code at all. **1,085 question cards**, each one
authored, fact-checked against a source, and language-polished (§5) — the model sees 5,306
lines of text to price as if they were code, but not the hours of curation behind them. And
everything it took for this to even exist at an address: buying the domain, configuring DNS,
writing the CI/CD pipeline in GitHub Actions, deploying to fly.io. Zero lines, in the model's
eyes. The largest part of the work weighs nothing.

That's the crux of it: **the model measures the shadow, not the thing casting it.** Lines of
code are the shadow. The thing casting the shadow — practised method — doesn't show up in a
SLOC counter.

## What Actually Made It Fast: Practised Method, Not AI Typing

The leverage wasn't that I typed fast, and it wasn't that an AI wrote it for me. It was that
*the decisions were already made* — by a set of senior disciplines I've **practised** until
they became reflex. Three of them are the backbone, and they all land in the same shape:
**Given–When–Then**.

### Vertical Slice Architecture

Build one feature at a time, all the way through: a self-contained slice. No code is shared
speculatively between features — every slice is an **independently verifiable
Given–When–Then contract**.

![[assets/vertical-slice.svg|700]]

For the lead: you can ship and verify one feature without rummaging through ten others. For
the engineer: no premature abstraction, no layers for layers' sake, coupling kept inside the
slice.

### Functional Core / Imperative Shell

All decision logic lives in a **pure core** — no databases, no clock, no network, fully
deterministic. Everything messy (I/O, time, calls) is pushed out into a thin outer shell.

![[assets/functional-core.svg|700]]

The consequence is right there in the image: *behaviour is simulatable — run thousands of
scenarios without a database, verifiable before it's even built.* You don't need to start
anything to know the logic holds. That's why the tests are bulletproof (more on that shortly).

### Decider + Event Sourcing

The game is a **Decider**: two total functions.

```
decide:  (State, Command)  →  Result<Event[]>
evolve:  (State, Event)    →  State
```

![[assets/decider-pattern.svg|700]]

The loop *is* Given–When–Then: **Given** a state (fold prior events via `evolve`), **When** a
command, **Then** events — or a rejection. The same shape as the slice, the same shape as the
test. Design and test speak the same language. (Initial and final states left out for brevity.)

All three patterns derive from **a single source**: an emlang event modeling spec that
maps the game's entire flow — trigger, command, event, view — before a line of code is written.

![[assets/game-flow-em.png|700]]

### The Supporting Beams

These aren't three loose patterns — they hang on a common scaffold:

- **Spec as source of truth.** All behaviour is defined first in an emlang spec (event
  modeling) — not in the code, not in my head. The code follows the spec.
- **Architectural Decision Records.** Every choice (in-memory event sourcing, Decider, HTMX polling, CSV, ROP,
  Razor static SSR) is a written decision with a *why*. Nothing is renegotiated in every
  commit.
- **Result / Railway-Oriented Programming** via native unions. Business failures are
  *values on a failure track*, never thrown exceptions. The core stays total.
- **Constraints as a feature.** No SignalR, no Entity Framework, no Blazor circuit. Fewer
  moving parts = fewer failure modes. The constraint is the design.
- **Determinism via `GameContext`.** Clock and id generator are *injected*. That's why the
  core is deterministic, and why the FC tests are bulletproof — clean and predictable, with no
  mocks at all.

None of this is talent. It's *practised engineering discipline*. It's repetition until the
decisions sit in the spinal cord and the cost of deciding goes to zero.

## Quality Isn't Claimed — It's Enforced

"Fast and cheap" means nothing if it breaks. So this isn't a claim about quality — it's a
*mechanism* for it.

- **Bulletproof FC tests.** Given–When–Then and Given–Then cases in `DeciderTests`,
  `EvolveTests`, `ProjectionTests`. The pure core makes them deterministic and mock-free — the test runs the same loop as the design.
- **Architecture tests.** `ArchitectureTests` enforces the FC/IS boundary *in CI*. If anyone
  sneaks I/O into the core, the build fails. The discipline is automated, not hopeful.
- **End-to-end.** `GameEndpointsTests` against a `TestAppFactory` runs all the way through the
  imperative shell.
- **EvalOps.** When a test fails, it gets fed back into memory — the failure becomes a
  permanent lesson, not a repeated miss.
- **Context management as a mechanism.** A `CLAUDE.md`, a constitution, an auto-memory. Not "I
  try to remember" — a written, loaded contract surface. The mechanism, not just the intention.
- **Simplification you don't need.** I run ponytail in Claude Code — an agent whose only job is
  to hunt for over-engineering and propose the simplest thing that works. The point: it almost
  never finds anything. Vertical slices, functional core, and constraints-as-feature prevent
  bloat *at the source* — the complexity is never written, so there's nothing to clean up after
  the fact. The cheapest simplification is the one you never had to make.

## And Not Just Code — Content Too

The question data isn't slapped together. Every card passes through a pipeline of specialised
agents before it reaches the live pack:

![[assets/agent-pipeline.svg|700]]

**author** writes batches against the difficulty bands → **fact-checker** verifies *every*
value and direction against a source and pins the year (unverifiable = rejected) →
**language-checker** polishes the Swedish without touching a single digit → **curator**
(`tools/pack.cs merge`) dedups and checks the band histogram into the live pack.

High quality, large human/AI effort — and **zero lines of code**. Just as invisible to COCOMO
as the architecture. Content is craft on the same terms as the software.


## Practise What You Preach

The skill *is* the sermon, and the sermon is practised. It was never the AI and never the
keystrokes — it was that the decisions were already made, by disciplines I'd drilled until they
became cheap. The method is the multiplier.

And here's the meta-proof: this article is itself an instance of the thesis. The argument, the
illustrations — drawn from my own pattern library, the same visual language as the game's
diagrams — and the drafting followed the exact same practised method as the code.

The anchor was never the point. The discipline is.

## Play

Enough theory — the game is made to be played. Gather your friends, open
[kvissig.se](https://kvissig.se), and guess. More or less?

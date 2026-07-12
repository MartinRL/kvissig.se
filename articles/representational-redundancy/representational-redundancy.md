# Representational Redundancy

> Last Friday I deleted 557 lines of C# from git. The build stayed green.

## The lines nobody misses

Last Friday I deleted 557 lines of C# from git. The build stayed green. All 181 tests
green. The web layer untouched.

The lines were the record layer of three games — commands, events, errors — and they
were deleted because they are no longer needed *as files*. They are generated on every
build, deterministically, from the same emlang spec that was always the source of
truth, by a Roslyn source generator straight into the compilation. They never exist on
disk. They cannot drift, because there is nothing left to drift *from*.

And here is the number that gives the game away: the net ledger in git came to
**+7 lines**. Transcription removed: ~701. Generator infrastructure added: 708. The
line count is a zero-sum game. So why bother?

Because the lines were never the point. The point is what those 557 lines *were*: the
same facts, stated one more time.

## The enemy has a name

How many places in your codebase know that a player has a name?

The entity. The DTO. The mapper between them. The SQL column. The migration that
created the column. The validator. The OpenAPI schema. The TypeScript interface. The
test fixture. Nine representations of **one** domain fact — and every behavioral change
is a coherent edit across nine sites at once.

<!-- TODO asset: fan-out diagram — one fact, nine representations, same visual language as decider-pattern.svg -->
![[assets/one-fact-nine-places.svg|700]]

This deserves its own name: **representational redundancy**. Not "layers", not
"boilerplate" — restatement. The same truth hand-transcribed between representations
that no compiler holds together.

Humans have always drifted on this — it is why "the documentation lies" became
folklore. But notice exactly what LLM agents are *worst* at: coherent editing across
many sites. An agent produces plausible code at every individual site; it is *between*
the sites that things break. So representational redundancy is not merely expensive the
way it always was — it is expensive precisely where the new workforce is weakest.

Deterministic derivation does not *manage* that redundancy. It **deletes** it.

## It was never the layers

Now for the objection I would shout from the back seat myself: *"oh, so architecture is
obsolete now, just YOLO everything into one file?"* No. The thesis is not anti-layer.

This repo keeps the hardest boundary there is — functional core / imperative shell —
and enforces it in CI with architecture tests. Dependency discipline *helps* agents:
small blast radius, testable seams, one rule ("the core touches no IO") that fails the
build when broken.

So separate two things that get lumped together in every clean/onion/n-tier debate:

- **A boundary** states a *rule*, once: dependencies point that way, never this way.
- **A tier** that restates the same fact — entity to DTO to mapper to schema — states
  no rule at all. It transcribes.

Boundaries are cheap and machine-checkable. Restatement is expensive and
machine-uncheckable. Conventional layered architectures with ORMs institutionalize the
restatement and call it discipline.

## Who transforms?

"Programming in English" is the tune of the times — vibe coding, spec kits, spec-first
IDEs. And they are right about half of it: intent, not code, should be the durable
artifact. But look at who performs the transformation from intent to code in every one
of those setups: **an LLM**. Markdown in, probabilistic code out. Every regeneration is
a fresh stochastic outcome; diffs don't compose; every run is a new review event. The
spec drift you meant to cure moves into the spec itself.

There is a ladder, and the rungs differ in *who transforms and what verifies*:

1. English → LLM → code, a human reviews everything. (Vibe coding — even the man who
   coined it scoped it to throwaway projects.)
2. Markdown spec → LLM → code + tests. Intent captured, transformation still
   stochastic.
3. **Formal spec → deterministic generator for the provable stratum + an agent writing
   the rest against compiler and test oracles.** ← where I moved last Friday.
4. Full formal synthesis. (Nobody serious is claiming it.)

<!-- TODO asset: the ladder, four rungs, transformer + verifier per rung -->
![[assets/transformer-ladder.svg|700]]

Every rung up buys *review-once* semantics for a larger stratum. The generator was
reviewed once and proved against all three games — after that, regeneration is a build
step, not a review event. Same spec in, byte-identical code out. CI can assert
`artifact == f(spec, generator)` as an invariant, not as a hope.

Dijkstra said it in 1978, about the idea of programming in natural language: formal
texts are effective precisely because their legitimacy can be checked by a few simple
rules — while natural language excels at making nonsense non-obvious. Forty-eight years
later, that is still the entire difference between rung 2 and rung 3.

## The agent is an amplifier

The expensive part of agentic engineering is no longer producing code — it is *knowing
it is right*. Verification is the bottleneck. Which makes the interesting property of
an architecture its **oracle density**: how fast, and how mechanically, is wrongness
detected?

In this repo: a new event in the spec becomes a compile error at every site that must
handle it (exhaustive unions, no default arms, warnings as errors). 181 pure
Given–When–Then tests run in under eight seconds — no mocks, no database, no
containers, because the core is two total functions. Compare the loop in a layered
stack: migrations, test containers, minutes per iteration, and the most important
failures surface at runtime — where the agent's feedback loop is weakest.

So here is where the thesis lands, and it is not a matter of taste: **the agent is an
amplifier of whatever verification regime already exists.** Amplified diffuse
verification yields plausible drift at machine speed. Amplified concentrated
verification yields checkable increments at machine speed. The architecture picks
which.

## Where I might be wrong

Three honest holes, before someone else finds them for me.

**The ghost of MDA.** 1:1 model→code with generated artifacts kept out of version
control is *exactly* what Model-Driven Architecture promised in the 2000s, and it died:
hand-edits broke the round-trip, the models grew as unwieldy as the code, the last 20%
never fit. The differences that must stay true here: the determinism was **proven
before** anything was flipped (shadow tests, zero divergences, three games); generated
code cannot be hand-edited *by construction* (it exists only inside the compilation);
and the escape valve is a typed seam where missing human code is a compile error — not
a "protected region" quietly rotting.

**The training data.** LLMs are most fluent where the training data is thickest:
mainstream layered CRUD. A project-local spec dialect is a zero-resource DSL — the
agent's raw fluency is highest exactly where I claim the architecture is worst. The
countermeasure is that the repo carries its own instruction set (spec cheat-sheet,
constitution, ADRs, fitness tests) — a fixed context cost instead of scattered reads
per change. Three flawless transcriptions and two zero-deviation flips say something.
But that is anecdote, not measurement. Nobody has published the benchmark.

**My own read side.** One fact still passes through spec → record → projection → view
model → Razor in my own codebase. Four, five representations. The thesis is only partly
realized in its own shop window. The next step of the experiment aims there — and if
the seams then start accumulating hand-maintained metadata until reviewing the spec
costs more than reviewing the code, the ghost of MDA has won and I will write that
article too.

## The contract, not the lines

If agent-generated code is cheap and regenerable — why build machinery to keep 557
lines out of git? Because the machinery's value was never the lines. It is the
**contract**: determinism turns regeneration into a build step instead of a review
event, and makes the spec the single change surface for the entire stratum, enforced by
the compiler instead of by discipline.

The leverage for agentic engineering is therefore not "spec instead of code", and not
"fewer layers". It is: **maximize the fraction of the system whose correctness is
decided by machine, and minimize representational redundancy in the rest.**
Programming-in-English over a layered-ORM stack is weak on both axes at once — the
transformation is stochastic and the verification is smeared. A formal spec with a
deterministic generator and a pure, exhaustive core is strong on both.

The enemy had a name all along. It just wasn't "layers".

## Play

The theory lives in a game, and the game is meant to be played. Grab your crew, open
[kvissig.se](https://kvissig.se) and see who guesses closest. More or less?

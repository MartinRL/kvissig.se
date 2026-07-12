# "The Spec Is the Product" Is a Slogan Until the Code Leaves Your Repo

> Everyone agrees verification is the bottleneck. Almost nobody draws the conclusion
> sitting in their `.gitignore`.

## The bottleneck everyone agrees on

Here is the one thing the whole industry currently agrees on: with LLM agents, producing
code is no longer the expensive part. *Knowing it is right* is. Verification is the
bottleneck. Every serious writer on agentic engineering has landed there — the debates
are about what to do about it.

The popular answer is "programming in English": vibe coding, spec kits, spec-first IDEs.
Capture intent in a markdown spec, let the agent transform it into code, keep the spec as
the durable artifact. And that answer is right about exactly half of it. Intent, not
code, *should* be the durable artifact.

But look at who performs the transformation in every one of those setups: **an LLM**.
Markdown in, probabilistic code out. Every regeneration is a fresh stochastic outcome;
diffs don't compose; every run is a new review event. You declared the spec the product —
and then wired the world's least deterministic compiler between the product and the thing
that ships. The verification bottleneck you set out to relieve is now *load-bearing* on
every regeneration.

There is a harder conclusion hiding under the consensus, and this article is about
drawing it. It ends with C# files that are not source code, and a `git rm` that made a
build *more* trustworthy.

## The enemy has a name

Start with a question you can answer about your own codebase right now: how many places
know that a customer has an email address?

The entity. The DTO. The AutoMapper profile. The EF Core configuration. The SQL column.
The migration that created the column. The FluentValidation rule. The OpenAPI schema. The
TypeScript interface. The test fixture. Ten representations of **one** domain fact — and
every behavioral change is a coherent edit across ten sites at once.

<!-- TODO asset: fan-out diagram — one fact, ten representations -->
![[assets/one-fact-nine-places.svg|700]]

This deserves its own name: **representational redundancy**. Not "layers", not
"boilerplate" — *restatement*. The same truth hand-transcribed between representations
that no compiler holds together.

Humans have always drifted on this — it is why "the documentation lies" became folklore.
But notice exactly what LLM agents are *worst* at: coherent editing across many sites. An
agent produces plausible code at every individual site; it is *between* the sites that
things break, and between the sites is precisely where no oracle lives. No compiler error
fires when the validator disagrees with the DTO. No test fails when the OpenAPI schema
drifts from the entity — until an integration test, minutes and containers away, maybe.

So representational redundancy is not merely expensive the way it always was. It is
expensive precisely where the new workforce is weakest, and cheap verification is
precisely what the new workforce needs most. Every restatement you delete is a class of
agent error that can no longer occur.

Deterministic derivation does not *manage* that redundancy. It **deletes** it.

## Who transforms, what verifies

There is a ladder, and the rungs differ in exactly two properties: *who performs the
transformation from intent to code, and what verifies the result*.

1. English → LLM → code, a human reviews everything. (Vibe coding — even the man who
   coined it scoped it to throwaway projects.)
2. Markdown spec → LLM → code + tests. Intent captured; transformation still stochastic;
   every regeneration still a review event.
3. **Formal spec → deterministic generator for the provable stratum + an agent writing
   the rest against compiler and test oracles.**
4. Full formal synthesis. (Nobody serious is claiming it.)

<!-- TODO asset: the ladder, four rungs, transformer + verifier per rung -->
![[assets/transformer-ladder.svg|700]]

Every rung up buys *review-once* semantics for a larger stratum. On rung 2 you review the
generated code every time, because you must — the transformer is a distribution, not a
function. On rung 3 you review the generator once, prove it against the spec, and from
then on regeneration is a build step. Same spec in, byte-identical code out. CI can
assert `artifact == f(spec, generator)` as an invariant, not as a hope.

Dijkstra said it in 1978, about the idea of programming in natural language: formal texts
are effective precisely because their legitimacy can be checked by a few simple rules —
while natural language excels at making nonsense non-obvious. Forty-eight years later,
that is still the entire difference between rung 2 and rung 3.

But rung 3 has a trap door, and everyone who has been in .NET long enough has fallen
through it.

## Three acts of generated code in .NET

**Act one, 2002.** Windows Forms v1 generated `InitializeComponent` straight into *your*
file, fenced off with a comment: `#region Windows Form Designer generated code` and a
stern "do not modify the contents of this method". Everyone modified the contents of this
method. The designer overwrote their edits, or worse, half-parsed them back. The
boundary between generated and human code was a *comment* — that is, a promise.

**Act two, 2005.** .NET 2.0 shipped partial classes, and the generated half moved into
`Form1.Designer.cs`. Entity Framework did the same dance with EDMX and T4:
`Model.Designer.cs`, thousands of lines of generated C#, sitting in your repo, in your
diffs, in your merge conflicts. The boundary was now a *file* — better. But the file was
still in git, so it was still editable, still reviewable, still mergeable, and under
deadline pressure somebody always did edit it, because the model was regenerated "later"
and later never came. MDA-era tooling called its version "protected regions" — marked
blocks where hand-written code was supposed to survive regeneration. They rotted
quietly, everywhere, and took the whole Model-Driven Architecture movement down with
them.

Both acts share one root cause: **the generated code was in the repository.** Anything in
the repo is, by the social contract of version control, source — reviewable, editable,
ownable. No comment fence and no file split can override that contract. As long as the
code is in git, "the model is the product" is a slogan, because the repo says otherwise
every time someone opens a diff.

**Act three is a Roslyn source generator.** The generator runs *inside the compiler*.
Its output exists only in the compilation — never on disk as a file you can open, edit,
or commit. Hand-editing the generated code is not forbidden by a comment or discouraged
by a file name; it is impossible *by construction*, the way editing the compiler's
register allocation is impossible. And the escape valve is typed: a `partial` method
seam, where *missing* human code is a compile error — not a protected region quietly
rotting.

This is the piece the "programming in English" conversation keeps missing, and it is C#'s
quiet, categorical advantage. Every stack has code generation — protobuf, OpenAPI
generators, Prisma — and nearly all of them emit files that end up committed, which puts
them permanently in act two. The principle is stack-agnostic; the *mechanism* is not. A
source generator makes "never versioned" a property of the toolchain instead of a
`.gitignore` discipline.

## The repo boundary is a decision boundary

Nobody commits `.o` files. Nobody commits `node_modules`, or compiled CSS, or the IL that
`csc` emits. Not because those artifacts are worthless — the shipped product is literally
made of them — but because they are *derivable*: a pure function of things already in the
repo. Versioning them would record no decision. Version control is for decisions.

Now apply that rule with a straight face: a C# record layer that is a pure, deterministic
function of a YAML spec **is a build artifact**. The `.cs` extension does not make it
source. *Source* is defined by causality — is this file where the decision lives? — not
by file type. If every byte of `Commands.cs` is determined by `spec.yaml` plus a
generator, then `Commands.cs` in git is a cached intermediate, checked in. We have a word
for committed caches that can drift from their inputs: bugs waiting.

Deleting those files from git is therefore not housekeeping. It is a forcing function —
the mechanism that turns the slogan structural:

- **The spec becomes the only change surface.** You cannot patch the generated code under
  deadline, because there is no file to patch. The act-two failure mode is not
  discouraged; it is gone.
- **Review collapses to two objects.** The spec (small, declarative, diffable) and the
  generator (reviewed once, proven, then frozen into a build step). Regeneration stops
  being a review event.
- **Drift becomes unrepresentable.** The generated representation cannot disagree with
  the spec, because it has no independent existence. There is nothing left to drift
  *from*.

That is what deleting representational redundancy actually means. Not DRY as a style
preference — the *removal of an entire category of state* in which the system could be
wrong.

## The training-data objection, inverted

The reflexive objection: LLMs are most fluent where training data is thickest —
mainstream layered CRUD — so a project-local spec dialect starves the agent exactly where
this architecture needs it to read and write specs.

The objection assumes the spec's *semantics* are as exotic as its file extension. They
are not. A spec for an event-sourced core is made of commands, events, business errors,
and Given–When–Then scenarios — which is to say: CQRS, BDD, and Gherkin, some of the most
heavily represented software concepts in any training corpus. Event modeling is a thin
arrangement notation over primitives every agent already knows cold. The surface syntax
is local; the semantics are high-resource.

And the payoff side of the trade is lopsided. What the agent gets in exchange for
learning a small dialect is an environment where its characteristic failure mode has been
deleted: a new event in the spec becomes a compile error at every site that must handle
it — exhaustive switches, no default arms, warnings as errors. Pure
Given–When–Then tests over total functions run in seconds, no mocks, no containers. The
agent is an amplifier of whatever verification regime already exists; amplified diffuse
verification yields plausible drift at machine speed, amplified concentrated verification
yields checkable increments at machine speed. Trading a sliver of syntactic fluency for
an order-of-magnitude denser oracle field is not a weakness to mitigate. It is the whole
point.

One honest line, though: nobody has published the benchmark. This is an argument, not a
measurement.

## Where I might be wrong

**The ghost of MDA is patient.** Acts one and two did not fail on day one; they failed
when reality's last 20% arrived and the seams started accumulating hand-maintained
metadata. If the typed seams of act three begin filling with so much human residue that
reviewing the spec costs more than reviewing code ever did, the ghost has won and the
correct move is to write *that* article.

**Determinism has to be proven, not assumed.** A generator with an unnoticed
nondeterminism — dictionary ordering, culture-sensitive formatting, a timestamp — turns
"build step" back into "review event" silently. The contract `artifact == f(spec,
generator)` is only worth what the round-trip test enforcing it is worth.

**The provable stratum is a stratum.** Records, serialization surfaces, exhaustive
unions — the parts of a system that are pure structure — derive beautifully. Behavior
does not, yet, and pretending otherwise is rung 4 cosplay. The claim is not "generate
everything"; it is "never hand-maintain what a function of the spec can emit, and give
the agent oracles for the rest".

## What happened when I did it

I run a workbench project for exactly these bets — a small production system, three
event-sourced games behind one web front, functional core / imperative shell, the
domain's behavior defined in a formal YAML spec dialect per game.

Last Friday, 557 lines of C# left the repository — and not scaffolding at the edges.
These were the domain vocabularies of all three games: every command, every event, every
business error. The load-bearing core that everything else — deciders, projections,
tests, the web layer — compiles against. A Roslyn source generator now emits them into
the compilation on every build, from the same specs that were always the source of
truth. The determinism was proven *before* anything was deleted: an emitter round-trip
against the previously committed files, zero divergences, three games in a row.

The ledger: 557 lines of hand-maintained core out of git, 708 lines of generator
infrastructure in — net **+7 lines**, with the third game's flip landing on the LOC
prediction to the digit. The build stayed green. All 181 pure Given–When–Then tests
stayed green, running in under eight seconds. The web layer never noticed.

Plus seven lines, and the repository is smaller in the way that matters: there are 557
fewer lines it is possible to be wrong in. The specs are no longer documentation that
compiles second. They are the only place the record layer exists at all — which is what
"the spec is the product" was supposed to mean before it was a slogan.

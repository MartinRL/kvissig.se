A 47-year-old cost model prices my weekend at $492,788.

I ran a line counter on a quiz game I built solo over a weekend. COCOMO (1981) said: half a million dollars, 10.51 months, 4.17 people.

Notice what that number just did to your expectations. It's an anchor (Kahneman & Tversky) — arbitrary and wrong, but now you measure everything against it. I name it on purpose, so I'm in on the joke.

The model measures the shadow, not the thing casting it. Lines of code are the shadow. It inflates (CSV, docs, generated code at 1981 rates) and it's blind (spec, architecture, test design ≈ 0 lines).

The leverage wasn't the AI, and it wasn't the keystrokes. It was practised method — senior disciplines drilled until they became reflex, all in the same shape: Given–When–Then.

→ Vertical Slice Architecture: every feature an independently verifiable contract.
→ Functional Core / Imperative Shell: a pure, deterministic core — verifiable before it's even built.
→ Decider + Event Sourcing: the loop IS Given–When–Then; design and test speak the same language.

Quality isn't claimed, it's enforced: bulletproof mock-free tests, the architecture boundary in CI. And content too — the question data is fact- and language-checked by an agent pipeline. All invisible to a line counter.

Practise what you preach: the skill IS the sermon, and the sermon is practised. This post is itself an instance of the thesis.

The anchor was never the point. The discipline is.

Longer version in the comments ↓

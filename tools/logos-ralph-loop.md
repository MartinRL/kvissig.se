# Ralph loop — logo corpus

Feed this to `/ralph-loop`. It terminates deterministically when the targets are met.

---

Prereq: `LOGODEV_TOKEN` is set, and `src/MerEllerMindre.Domain/data/logos/logos.csv`
has been reviewed (run `dotnet run tools/logos.cs -- gen` to rebuild it from
`tools/logos-seed.csv` after edits).

Each iteration:

1. Run `dotnet run tools/logos.cs -- fetch --limit 150` from the repo root. This downloads
   the next batch of rows that have no file yet, validates each as a real PNG, writes
   `data/logos/{se,int}/<slug>.png`, and rewrites `failures.csv` to exactly the rows still
   missing a file.
2. Spot-check 2–3 freshly downloaded files with the Read tool — confirm they are real,
   non-empty COLOR logos, not blank/monogram placeholders. If a file is a placeholder,
   delete it and add its row to the rescue work below.
3. For each row in `failures.csv`, rescue it (write `data/logos/{origin}/<slug>.png` —
   `.svg` is acceptable from Wikimedia) using your judgement, trying in order:
   a. a corrected/alternate domain re-run through the fetch URL
      `https://img.logo.dev/<domain>?token=$LOGODEV_TOKEN&format=png&size=256&retina=true`,
   b. the company's own website logo,
   c. Wikipedia / Wikimedia Commons.
   Keep the SAME slug. After rescuing, re-run step 1 (fetch rewrites `failures.csv`, so a
   row with a file on disk drops out automatically).
4. Run `dotnet run tools/logos.cs -- status` and report: se count, int count, remaining to
   2000. If it prints "DONE — targets met" (se ≥ 700 and total ≥ 2000), STOP. Otherwise
   continue.

If `logos.csv` runs out of rows before hitting 2000 / 700-se, STOP and report the gap —
the loop fetches, it does not invent brands. Add more rows to `tools/logos-seed.csv`,
re-run `gen`, then resume the loop.

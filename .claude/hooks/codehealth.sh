#!/usr/bin/env bash
# CodeScene CodeHealth gate. Mirrors arch-fitness.sh: on failure block the stop (exit 2)
# and feed the sub-threshold files back to the agent so it self-corrects.
# Fix the CODE (reduce complexity, split methods, name things) — NEVER lower the threshold.
#
# Modes:
#   --changed   Stop-hook / local: only scope files touched vs HEAD (+ staged). Fast gate.
#   --all       CI / baseline: whole prod-C# set.
#   --report    Read-only baseline: per-file CH grouped Backend/Frontend, min/mean, files < THRESHOLD. No exit 2.
#
# Requires: cs (CodeScene devtools CLI) on PATH + CS_ACCESS_TOKEN set. jq for parsing.
# CH schema (cs 1.0.33): `cs review <file> --output-format json` -> { "score": <0-10>, "review": [...] }.
# One file per invocation; score null = "no scorable code" (e.g. record-only files) -> skipped.
set -uo pipefail

# Claude's Bash tool and the Stop hook run non-interactive bash that does NOT source ~/.bashrc,
# so cs would get no token and the gate would silently pass. Pull the token in if it's missing.
# CI injects CS_ACCESS_TOKEN as env, so this is a no-op there.
[ -z "${CS_ACCESS_TOKEN:-}" ] && source ~/.bashrc 2>/dev/null

THRESHOLD=9.4

# Documented per-file exemptions: presentation screen-selectors sit at CH 9.38 — the exhaustive
# per-screen render dispatch that rounds to 9.4. Splitting them would only scatter the switch,
# not remove complexity, so they are grandfathered (like the mini-deck 1085-card exemption).
# TankScreens.cs is the same pattern (Tänk Till Tusen's 6-screen selector, CH 9.38).
# NEVER lower the global THRESHOLD; add a line here (with the CH + reason) instead.
EXEMPT_RE='src/MerEllerMindre\.Web/Presentation/(Auction|Game|Tank)Screens\.cs'

mode="${1:---changed}"

# stop_hook_active guard (only relevant when invoked as a Stop hook, which passes JSON on stdin).
if [ "$mode" = "--changed" ] && [ ! -t 0 ]; then
  input="$(cat)"
  case "$input" in
    *'"stop_hook_active":true'*) exit 0 ;;
  esac
fi

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}" || exit 0

# Prod C# scope: backend Domains + Web .cs. Exclude obj/bin/Tests/generated.
# Tracked + untracked-not-ignored, so a brand-new feature's files are scored BEFORE first commit
# (regression 2026-07-06: git-tracked-only silently passed the whole new TankTillTusen.Domain).
# Trailing slash after ".Domain" excludes the sibling ".Domain.Tests/" dirs.
# NOTE: every new game's Domain project MUST be added to the regex or its files are never scored.
scope_files() {
  { git ls-files -- '*.cs'; git ls-files --others --exclude-standard -- '*.cs'; } | sort -u \
    | grep -E '^src/(MerEllerMindre\.Domain|Blindbudet\.Domain|TankTillTusen\.Domain|MerEllerMindre\.Web|Emlang\.(CodeGen|Generators))/' \
    | grep -viE '(/obj/|/bin/|\.Tests/|\.g\.cs$)'
}

is_backend() { case "$1" in src/*.Domain/*) return 0 ;; *) return 1 ;; esac; }

# Emit "path<TAB>score" for the given files. cs review scores ONE file per call; null score
# (record-only / no scorable code) is skipped.
score_files() {
  for f in "$@"; do
    s="$(cs review "$f" --output-format json 2>/dev/null | jq -r '.score // empty')"
    [ -n "$s" ] && printf '%s\t%s\n' "$f" "$s"
  done
}

# Below-threshold awk filter: prints "path score" for score < THRESHOLD.
below() { awk -F'\t' -v t="$THRESHOLD" '$2 != "" && $2+0 < t {printf "%s: CH %.2f (< %s)\n", $1, $2, t}'; }

case "$mode" in
  --report)
    all="$(scope_files)"
    scored="$(score_files $all)"
    for group in Backend Frontend; do
      echo "== $group =="
      sum=0; n=0; min=99
      while IFS=$'\t' read -r f s; do
        [ -z "$f" ] && continue
        if [ "$group" = Backend ]; then is_backend "$f" || continue; else is_backend "$f" && continue; fi
        printf "  %-60s CH %s\n" "$f" "$s"
        sum=$(awk -v a="$sum" -v b="$s" 'BEGIN{print a+b}'); n=$((n+1))
        min=$(awk -v a="$min" -v b="$s" 'BEGIN{print (b<a)?b:a}')
      done <<< "$scored"
      [ "$n" -gt 0 ] && awk -v s="$sum" -v n="$n" -v m="$min" 'BEGIN{printf "  -- min %.1f  mean %.2f  (%d files)\n", m, s/n, n}'
    done
    echo "== Files < $THRESHOLD =="
    echo "$scored" | below | sed 's/^/  /'
    exit 0
    ;;
  --all)
    files="$(scope_files)"
    ;;
  --changed)
    changed="$( { git diff --name-only HEAD; git diff --name-only --cached; \
                  git ls-files --others --exclude-standard; } | sort -u )"
    files="$(comm -12 <(scope_files | sort) <(echo "$changed" | sort))"
    ;;
  *)
    echo "usage: codehealth.sh [--changed|--all|--report]" >&2; exit 2 ;;
esac

[ -z "$files" ] && exit 0

failures="$(score_files $files | below | grep -vE "$EXEMPT_RE")"
if [ -n "$failures" ]; then
  echo "CodeHealth gate FAILED — raise the CODE to CH >= $THRESHOLD, do NOT lower the threshold:" >&2
  echo "$failures" >&2
  exit 2
fi
exit 0

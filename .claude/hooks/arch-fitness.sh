#!/usr/bin/env bash
# Stop hook: run the architecture fitness tests at every turn end. On failure, block the
# stop (exit 2) and feed the failing assertions back to the agent so it self-corrects.
# Fix the offending CODE — never weaken the tests.
set -uo pipefail

input="$(cat)"

# Loop guard: don't re-block a stop that is itself a hook continuation.
case "$input" in
  *'"stop_hook_active":true'*) exit 0 ;;
esac

cd "$CLAUDE_PROJECT_DIR" || exit 0

output="$(dotnet test --filter "FullyQualifiedName~ArchitectureTests" --nologo 2>&1)"
status=$?

if [ "$status" -ne 0 ]; then
  echo "Architecture fitness tests FAILED — fix the offending code, do NOT weaken the tests:" >&2
  echo "$output" | grep -E "\[FAIL\]|Error Message|Expected|Failed!" >&2
  exit 2
fi

exit 0

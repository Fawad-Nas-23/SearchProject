#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://127.0.0.1:5190"
TOTAL_AGENTS=1000
PARALLEL=50

if [ -z "${1:-}" ]; then
  echo "Brug: $0 <tekstfil>"
  exit 1
fi

FILE="$1"

if [ ! -f "$FILE" ]; then
  echo "FEJL: fil ikke fundet"
  exit 1
fi

echo "=== SearchAgent Stress Test (deterministic round-robin) ==="

# ----------------------------
# Extract words
# ----------------------------
mapfile -t WORDS < <(
  tr -d '[:punct:]' < "$FILE" |
  tr '[:upper:]' '[:lower:]' |
  tr -s ' ' '\n' |
  awk 'length($0) > 0'
)

WORD_COUNT=${#WORDS[@]}

if (( WORD_COUNT == 0 )); then
  echo "Ingen ord fundet"
  exit 1
fi

echo "Words: $WORD_COUNT"
echo "Creating: $TOTAL_AGENTS agents"
echo ""

# ----------------------------
# Create agent function
# ----------------------------
create_agent () {
  i=$1

  WORD="${WORDS[$(( (i - 1) % WORD_COUNT ))]}"

  JSON="{\"email\":\"stresstest${i}@mail.dk\",\"searchWords\":[\"$WORD\"]}"

  curl -s -X POST "$BASE_URL/api/searchagent" \
    -H "Content-Type: application/json" \
    -d "$JSON" > /dev/null
}

export -f create_agent
export BASE_URL
export WORDS
export WORD_COUNT

# ----------------------------
# CREATE AGENTS (parallel)
# ----------------------------
START=$(date +%s)

seq 1 $TOTAL_AGENTS | xargs -P $PARALLEL -I{} bash -c 'create_agent "$@"' _ {}

END=$(date +%s)

echo ""
echo "Agent creation time: $((END - START))s"

# ----------------------------
# VERIFY COUNT
# ----------------------------
COUNT=$(curl -s "$BASE_URL/api/searchagent" | grep -o '"email"' | wc -l)
echo "Total agents in system: $COUNT"

echo ""
echo "=== Running agents ==="

RUN_START=$(date +%s)

RESULT=$(curl -s -X POST "$BASE_URL/api/searchagent/run")

TOTAL=$(echo "$RESULT" | grep -o '"agentId"' | wc -l)
MATCHES=$(echo "$RESULT" | grep -o '"matchFound":true' | wc -l)
NO_MATCH=$(echo "$RESULT" | grep -o '"matchFound":false' | wc -l)

RUN_END=$(date +%s)

echo ""
echo "Results:"
echo "  Total:   $TOTAL"
echo "  Match:   $MATCHES"
echo "  No match:$NO_MATCH"
echo "  Time:    $((RUN_END - RUN_START))s"

echo ""
echo "=== Metrics ==="
curl -s "$BASE_URL/metrics" | grep searchapi_cache || echo "No metrics found"

echo "=== Done ==="
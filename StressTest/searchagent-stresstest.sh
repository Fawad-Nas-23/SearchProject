#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://10.10.139.94:8082"
TOTAL_AGENTS=500
PARALLEL_CREATION=50

echo "=== SearchAgent Stress Test (curl only, GitBash safe) ==="
echo "Creating $TOTAL_AGENTS agents with concurrency..."

create_agent () {
    i=$1

    # Re-declare WORDS inside subshell (critical for Git Bash)
    WORDS=(
        report meeting revenue growth market
        trading London profit technology cloud
        server infrastructure engineering migration energy
        contract pipeline gas portfolio management
        employee training developer remote productivity
        legal merger compliance security privacy
        marketing campaign enterprise healthcare education
        supply warehouse automation inventory demand
        research patent artificial intelligence machine
        financial budget innovation sustainable stakeholder
    )

    NUM_WORDS=$(( (RANDOM % 3) + 1 ))
    SELECTED=()

    for _ in $(seq 1 $NUM_WORDS); do
        idx=$((RANDOM % ${#WORDS[@]}))
        SELECTED+=("${WORDS[$idx]}")
    done

    WORD_ARRAY=$(printf '"%s",' "${SELECTED[@]}")
    WORD_ARRAY="[${WORD_ARRAY%,}]"

    JSON="{\"email\":\"agent${i}@stress.dk\",\"searchWords\":${WORD_ARRAY}}"

    curl -s -X POST "$BASE_URL/api/searchagent" \
        -H "Content-Type: application/json" \
        -d "$JSON" > /dev/null
}

export -f create_agent
export BASE_URL

START=$(date +%s)

seq 1 $TOTAL_AGENTS | xargs -P $PARALLEL_CREATION -I{} bash -c 'create_agent "$@"' _ {}

END=$(date +%s)

echo "Agent creation took $((END - START)) seconds"
echo ""

echo "Verifying agent count..."
COUNT=$(curl -s "$BASE_URL/api/searchagent" | grep -o '"email"' | wc -l)
echo "  Total agents: $COUNT"

echo ""
echo "=== Running all agents ==="

RUN_START=$(date +%s)

RESULT=$(curl -s -X POST "$BASE_URL/api/searchagent/run")

TOTAL=$(echo "$RESULT" | grep -o '"agentId"' | wc -l)
MATCHES=$(echo "$RESULT" | grep -o '"matchFound":true' | wc -l)
NO_MATCH=$(echo "$RESULT" | grep -o '"matchFound":false' | wc -l)

echo "  Total:      $TOTAL"
echo "  Matches:    $MATCHES"
echo "  No match:   $NO_MATCH"
echo "  Emails sent: $MATCHES"

RUN_END=$(date +%s)
echo "Run took $((RUN_END - RUN_START)) seconds"

echo ""
echo "=== Remaining agents (no match) ==="
REMAINING=$(curl -s "$BASE_URL/api/searchagent" | grep -o '"email"' | wc -l)
echo "  Remaining: $REMAINING agents"

echo ""
echo "=== Check Prometheus metrics ==="
curl -s "$BASE_URL/metrics" 2>/dev/null | grep searchapi_cache || echo "  Metrics not available"

echo ""
echo "=== Done ==="
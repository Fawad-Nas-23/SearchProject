#!/usr/bin/env bash
set -uo pipefail

BASE_URL="http://127.0.0.1:5190"
BODIES_DIR="bodies"
TOTAL_AGENTS=1000
PARALLEL=50

if [ ! -d "$BODIES_DIR" ]; then
  echo "FEJL: Mappen '$BODIES_DIR' blev ikke fundet"
  exit 1
fi

FILES=("$BODIES_DIR"/*.json)

if [ ! -e "${FILES[0]}" ]; then
  echo "FEJL: Ingen JSON-filer fundet i '$BODIES_DIR'"
  exit 1
fi

FILES_COUNT=${#FILES[@]}

echo "=== SearchAgent Stress Test ==="
echo "Base URL:      $BASE_URL"
echo "Bodies folder: $BODIES_DIR"
echo "JSON files:    $FILES_COUNT"
echo "Agents:        $TOTAL_AGENTS"
echo "Parallel:      $PARALLEL"
echo ""

echo "Tester forbindelse til SearchAgent..."

HEALTH_TEST=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/searchagent")

if [ "$HEALTH_TEST" = "000" ]; then
  echo "FEJL: Kan ikke forbinde til $BASE_URL"
  echo "Tjek at denne kører i en anden terminal:"
  echo "kubectl port-forward svc/searchagent 5190:80"
  exit 1
fi

echo "Forbindelse OK. HTTP status: $HEALTH_TEST"
echo ""

create_agent_task () {
  AGENT_NUM="$1"
  FILE_PATH="$2"

  WORD=$(grep -oP '"query":\s*\["\K[^"]+' "$FILE_PATH" | head -1 || true)

  if [ -z "$WORD" ]; then
    WORD=$(sed -n 's/.*"query":\s*\["\([^"]*\)".*/\1/p' "$FILE_PATH" | head -1)
  fi

  if [ -z "$WORD" ]; then
    echo "ERROR|agent${AGENT_NUM}|Kunne ikke finde query word i $FILE_PATH"
    return 1
  fi

  JSON="{\"Email\":\"agent${AGENT_NUM}@stress.dk\",\"SearchWords\":[\"$WORD\"]}"

  HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE_URL/api/searchagent" \
    -H "Content-Type: application/json" \
    -d "$JSON")

  if [ "$HTTP_STATUS" = "200" ] || [ "$HTTP_STATUS" = "201" ] || [ "$HTTP_STATUS" = "204" ]; then
    echo "OK|agent${AGENT_NUM}|$WORD|$HTTP_STATUS"
  else
    echo "ERROR|agent${AGENT_NUM}|$WORD|HTTP $HTTP_STATUS"
  fi
}

export -f create_agent_task
export BASE_URL

echo "Creating $TOTAL_AGENTS agents..."
START_CREATE=$(date +%s)

TEMP_RESULT_FILE="stress_result_$(date +%s).txt"

for i in $(seq 0 $((TOTAL_AGENTS - 1))); do
  file_index=$(( i % FILES_COUNT ))
  echo "$((i + 1))|${FILES[$file_index]}"
done | xargs -P "$PARALLEL" -I{} bash -c '
  IFS="|" read -r agent_id file_path <<< "$1"
  create_agent_task "$agent_id" "$file_path"
' _ {} | tee "$TEMP_RESULT_FILE"

END_CREATE=$(date +%s)

CREATED_OK=$(grep -c "^OK|" "$TEMP_RESULT_FILE" || true)
CREATED_ERRORS=$(grep -c "^ERROR|" "$TEMP_RESULT_FILE" || true)

echo ""
echo "Creation summary:"
echo "  Created OK: $CREATED_OK"
echo "  Errors:     $CREATED_ERRORS"
echo "  Time:       $((END_CREATE - START_CREATE))s"

echo ""
echo "Verifying agents in system..."

COUNT=$(curl -s "$BASE_URL/api/searchagent" | grep -o '"email"' | wc -l || echo "0")

echo "Total agents in system: $COUNT"

echo ""
echo "Triggering agent run..."

RUN_START=$(date +%s)

RESULT=$(curl -s -X POST "$BASE_URL/api/searchagent/run")

RUN_END=$(date +%s)

MATCHES=$(echo "$RESULT" | grep -o '"matchFound":true' | wc -l || echo "0")

echo ""
echo "Run results:"
echo "  Matches Found: $MATCHES"
echo "  Run Time:      $((RUN_END - RUN_START))s"

echo ""
echo "=== Done ==="

rm -f "$TEMP_RESULT_FILE"
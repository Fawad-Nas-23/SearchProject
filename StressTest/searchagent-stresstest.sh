#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://127.0.0.1:5190"
BODIES_DIR="bodies"
ITERATIONS=10
PARALLEL=50

if [ ! -d "$BODIES_DIR" ]; then
  echo "FEJL: Mappen '$BODIES_DIR' blev ikke fundet"
  exit 1
fi

# Collect all json files into an array
FILES=("$BODIES_DIR"/*.json)
FILES_COUNT=${#FILES[@]}

if [ "$FILES_COUNT" -eq 0 ]; then
  echo "FEJL: Ingen JSON filer fundet i $BODIES_DIR"
  exit 1
fi

echo "=== SearchAgent Stress Test (1000 Agents / 10x Iteration) ==="

# ----------------------------
# Create agent function
# ----------------------------
create_agent_task () {
  AGENT_NUM=$1
  FILE_PATH=$2
  
  # Extract word from Orchestrator JSON format
  WORD=$(grep -oP '"query":\s*\["\K[^"]+' "$FILE_PATH" | head -1)
  
  if [ -z "$WORD" ]; then
    WORD=$(cat "$FILE_PATH" | sed -n 's/.*"query":\["\([^"]*\)".*/\1/p')
  fi

  # JSON matching your SearchAgentRequest model
  # Email now uses the unique agent number
  JSON="{\"Email\":\"agent${AGENT_NUM}@stress.dk\",\"SearchWords\":[\"$WORD\"]}"

  curl -s -X POST "$BASE_URL/api/searchagent" \
    -H "Content-Type: application/json" \
    -d "$JSON" > /dev/null
}

export -f create_agent_task
export BASE_URL

# ----------------------------
# 1. CREATE 1000 AGENTS
# ----------------------------
echo "Creating 1000 agents using 10 iterations of $FILES_COUNT files..."
START_CREATE=$(date +%s)

# Generate a sequence from 0 to 999
# We calculate which file to use via modulo: index = agent_num % files_count
seq 0 999 | xargs -P "$PARALLEL" -I{} bash -c '
  agent_id=$1
  files_arr=("$BODIES_DIR"/*.json)
  num_files=${#files_arr[@]}
  file_index=$(( agent_id % num_files ))
  
  create_agent_task "$((agent_id + 1))" "${files_arr[$file_index]}"
' _ {}

END_CREATE=$(date +%s)
echo "Creation took $((END_CREATE - START_CREATE))s"

# ----------------------------
# 2. VERIFY & RUN
# ----------------------------
echo ""
COUNT=$(curl -s "$BASE_URL/api/searchagent" | grep -o '"email"' | wc -l)
echo "Total agents in system: $COUNT"

echo "Triggering agent run..."
RUN_START=$(date +%s)
RESULT=$(curl -s -X POST "$BASE_URL/api/searchagent/run")
RUN_END=$(date +%s)

MATCHES=$(echo "$RESULT" | grep -o '"matchFound":true' | wc -l || echo "0")

echo "Results:"
echo "  Matches Found: $MATCHES"
echo "  Run Time:      $((RUN_END - RUN_START))s"
echo "=== Done ==="
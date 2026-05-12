#!/bin/bash

ENDPOINT="http://localhost:8082/api/orchestrator/search"

echo "=== Stress Test: 100 unikke ord, 10 concurrent ==="
echo ""

TOTAL=0
ERRORS=0

for f in bodies/*.json; do
  WORD=$(cat "$f" | grep -o '"[a-z]*"' | head -1)
  
  RESULT=$(hey -m POST \
      -H "Content-Type: application/json" \
      -D "$f" \
      -n 10 \
      -c 10 \
      -q 20 \
      "$ENDPOINT" 2>&1)

  STATUS=$(echo "$RESULT" | grep "\[200\]" | awk '{print $2}')
  AVG=$(echo "$RESULT" | grep "Average:" | awk '{print $2}')

  echo "  $WORD → ${STATUS:-0} ok | avg: ${AVG:-error}"
  TOTAL=$((TOTAL + 1))
done

echo ""
echo "=== Færdig: $TOTAL ord testet ==="
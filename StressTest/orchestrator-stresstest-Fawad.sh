#!/bin/bash

ENDPOINT="http://127.0.0.1:8082/api/orchestrator/search"
TOTAL_PASSES=5

if [ -z "$1" ]; then
  echo "Brug: $0 <tekstfil>"
  exit 1
fi

FILE="$1"

if [ ! -f "$FILE" ]; then
  echo "FEJL: Filen '$FILE' blev ikke fundet."
  exit 1
fi

echo ""
echo "============================================================"
echo "  Indlæser og normaliserer ord fra: $FILE"
echo "============================================================"

mapfile -t WORDS < <(
  tr -d '[:punct:]' < "$FILE" |
  tr '[:upper:]' '[:lower:]' |
  xargs -n1 |
  awk 'length($0) > 0'
)

WORD_COUNT=${#WORDS[@]}

echo "  Antal ord fundet: $WORD_COUNT"
echo ""

if (( WORD_COUNT == 0 )); then
  echo "FEJL: Ingen ord kunne udtrækkes fra filen."
  exit 1
fi

echo "============================================================"
echo "  Starter stresstest: $WORD_COUNT ord x $TOTAL_PASSES passes"
echo "  Endpoint: $ENDPOINT"
echo "============================================================"
echo ""

GLOBAL_START=$(date +%s)

PASS_TOTAL_TIMES=()

for PASS in $(seq 1 $TOTAL_PASSES); do

  echo "------------------------------------------------------------"
  echo "  PASS $PASS / $TOTAL_PASSES  –  $(date '+%H:%M:%S')"
  echo "------------------------------------------------------------"

  PASS_START=$(date +%s%N)
  PASS_ERRORS=0
  PASS_TIME_SUM=0

  for ((i=0; i<WORD_COUNT; i++)); do
    WORD="${WORDS[$i]}"
    INDEX=$((i + 1))

    RESPONSE=$(curl -s -m 10 -o /dev/null \
      -w "%{http_code} %{time_total}\n" \
      -X POST "$ENDPOINT" \
      -H "Content-Type: application/json" \
      -d "{\"query\":[\"$WORD\"],\"maxAmount\":20,\"caseSensitive\":false}")

    HTTP_STATUS="${RESPONSE% *}"
    TIME_SEC="${RESPONSE#* }"
    TIME_MS=$(awk "BEGIN {printf \"%.0f\", $TIME_SEC * 1000}")

    PASS_TIME_SUM=$((PASS_TIME_SUM + TIME_MS))

    if [[ "$HTTP_STATUS" != "200" ]]; then
      PASS_ERRORS=$((PASS_ERRORS + 1))
      echo "  [Pass $PASS] [$INDEX/$WORD_COUNT] FEJL: '$WORD' → HTTP $HTTP_STATUS (${TIME_MS}ms)"
    fi

    if (( INDEX % 10 == 0 || INDEX == WORD_COUNT )); then
      echo "  [Pass $PASS] [$INDEX/$WORD_COUNT] Seneste: '$WORD' → HTTP $HTTP_STATUS | ${TIME_MS}ms | Akk. tid: ${PASS_TIME_SUM}ms"
    fi

  done

  PASS_END=$(date +%s%N)
  PASS_DURATION_MS=$(( (PASS_END - PASS_START) / 1000000 ))
  PASS_DURATION_SEC=$(awk "BEGIN {printf \"%.2f\", $PASS_DURATION_MS / 1000}")
  AVG_MS=$(( PASS_TIME_SUM / WORD_COUNT ))

  PASS_TOTAL_TIMES+=("$PASS_DURATION_MS")

  echo ""
  echo "  ✓ Pass $PASS afsluttet på ${PASS_DURATION_SEC}s"
  echo "    → Fejl: $PASS_ERRORS / $WORD_COUNT"
  echo "    → Gennemsnitlig responstid: ${AVG_MS}ms pr. request"
  echo ""

done

GLOBAL_END=$(date +%s)
TOTAL_SEC=$((GLOBAL_END - GLOBAL_START))

echo "============================================================"
echo "  STRESSTEST FÆRDIG – Total tid: ${TOTAL_SEC}s"
echo "============================================================"
echo ""
echo "  Cache-effekt sammenligning:"

for PASS in $(seq 1 $TOTAL_PASSES); do
  IDX=$((PASS - 1))
  MS="${PASS_TOTAL_TIMES[$IDX]}"
  SEC=$(awk "BEGIN {printf \"%.2f\", $MS / 1000}")
  echo "    Pass $PASS: ${SEC}s  (${MS}ms)"
done

P1="${PASS_TOTAL_TIMES[0]}"
P3="${PASS_TOTAL_TIMES[2]}"

if (( P1 > 0 && P3 > 0 )); then
  SPEEDUP=$(awk "BEGIN {printf \"%.2f\", $P1 / $P3}")
  echo ""
  echo "  → Pass 3 var ca. ${SPEEDUP}x hurtigere end Pass 1 (cache-gevinst)"
fi

echo ""
echo "============================================================"
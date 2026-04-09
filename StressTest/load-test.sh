#!/bin/bash

URL="http://127.0.0.1:64573/api/search"
REQUESTS=100

echo "Sender $REQUESTS requests til $URL..."

for i in $(seq 1 $REQUESTS); do
  curl -s -o /dev/null -X POST "$URL" \
    -H "Content-Type: application/json" \
    -d '{"query": ["hello"], "maxAmount": 10, "caseSensitive": false}'
  echo "Request $i sendt"
done

echo "Stress-test færdig!"
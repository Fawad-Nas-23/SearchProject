#!/bin/bash

# Configuration
ENDPOINT="http://10.10.139.94:8082/api/orchestrator/search"

# The text source
TEXT="Subject: Quarterly Report Meeting From: john.smith@company.com To: team@company.com Date: March 15, 2024 Dear Team, Please find attached the quarterly financial report for review. The revenue numbers show significant growth in the European market, while the Asian market experienced moderate decline. Our trading operations in London performed exceptionally well, with profit margins exceeding expectations. Key highlights from the report: The technology division delivered strong results with cloud computing services growing rapidly. Server infrastructure investments paid off with reduced downtime and improved customer satisfaction. The engineering team completed the migration project ahead of schedule. Energy sector partnerships continue to expand. The contract with Pacific Gas and Electric was renewed for another five years. Natural gas prices remained stable throughout the quarter, benefiting our portfolio management strategy. Human resources reported increased employee retention rates. The new training program for junior developers received positive feedback. Remote work policies contributed to higher productivity across all departments. The legal department finalized the merger acquisition documents. Compliance audits were completed successfully with no major findings. International regulations regarding data privacy required updates to our security infrastructure. Marketing launched several successful campaigns targeting enterprise customers. Brand awareness metrics improved significantly in the healthcare and education sectors. Digital advertising spend was optimized resulting in better conversion rates. Supply chain operations maintained efficiency despite global shipping challenges. Warehouse automation reduced processing time by thirty percent. Inventory management systems were upgraded to handle increased demand. The research and development team filed twelve new patent applications. Artificial intelligence projects showed promising results in predictive analytics. Machine learning models for customer behavior analysis reached production readiness. Financial planning indicates strong outlook for the next quarter. Budget allocations prioritize innovation and sustainable growth. Stakeholder confidence remains high based on current performance indicators. Best regards, John Smith Chief Executive Officer"

# 1. Clean the text, get unique words, and shuffle
echo "Preparing unique search terms..."
WORDS=$(echo "$TEXT" | tr -d '[:punct:]' | tr '[:upper:]' '[:lower:]' | xargs -n1 | sort -u | shuf)
TOTAL_WORDS=$(echo "$WORDS" | wc -l)

echo "Starting Stress Test: $TOTAL_WORDS words, 5 passes each (Total $((TOTAL_WORDS * 5)) requests)"
echo "------------------------------------------------------------"

GLOBAL_START=$(date +%s)

# Outer loop to run the entire set 5 times
for PASS in {1..5}; do
    echo ">>> Starting Pass #$PASS"
    PASS_START=$(date +%s.%N)
    
    COUNT=0
    for WORD in $WORDS; do
        COUNT=$((COUNT + 1))
        
        # Send request
        RESPONSE=$(curl -s -o /dev/null -w "%{http_code} %{time_total}" -X POST "$ENDPOINT" \
             -H "Content-Type: application/json" \
             -d "{\"query\":[\"$WORD\"],\"maxAmount\":20,\"caseSensitive\":false}")

        # Log progress every 25 requests
        if (( COUNT % 25 == 0 )); then
            STATUS=${RESPONSE% *}
            TIME=${RESPONSE#* }
            echo "[Pass $PASS] [$COUNT/$TOTAL_WORDS] Word: '$WORD' | Status: $STATUS | Time: ${TIME}s"
        fi
    done
    
    PASS_END=$(date +%s.%N)
    # Basic math for pass duration (works on most modern Linux systems)
    DURATION=$(echo "$PASS_END - $PASS_START" | bc 2>/dev/null || echo "Done")
    echo ">>> Pass #$PASS finished. Duration: ${DURATION}s"
    echo "------------------------------------------------------------"
done

GLOBAL_END=$(date +%s)
echo "Full Stress Test Complete!"
echo "Total Execution Time: $((GLOBAL_END - GLOBAL_START)) seconds"
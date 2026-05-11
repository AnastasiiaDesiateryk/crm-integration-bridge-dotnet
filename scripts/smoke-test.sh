#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"

echo "Smoke test: Website Status Bridge"
echo "BASE_URL=$BASE_URL"

echo
echo "1) Bridge health check"

HEALTH_RESPONSE="$(curl -sS "$BASE_URL/health")"
echo "$HEALTH_RESPONSE" | jq .

STATUS="$(echo "$HEALTH_RESPONSE" | jq -r '.status')"
SERVICE="$(echo "$HEALTH_RESPONSE" | jq -r '.service')"
UPSTREAM="$(echo "$HEALTH_RESPONSE" | jq -r '.upstream // empty')"

if [[ "$STATUS" != "UP" ]]; then
  echo "Expected health status UP, got: $STATUS"
  exit 1
fi

if [[ "$SERVICE" != "website-status-bridge" ]]; then
  echo "Expected service website-status-bridge, got: $SERVICE"
  exit 1
fi

if [[ -z "$UPSTREAM" ]]; then
  echo "Expected upstream URL in health response"
  exit 1
fi

echo "Bridge health check passed"
echo "Upstream: $UPSTREAM"

echo
echo "2) Website status check through bridge"
echo "Note: bridge should wake up the upstream Enricher API automatically if needed."

CHECK_RESPONSE="$(curl -sS -X POST "$BASE_URL/api/website-status/check" \
  -H "Content-Type: application/json" \
  -d '{
    "organizations": [
      {
        "organizationId": "org-working",
        "company": "Google",
        "website": "https://google.com"
      },
      {
        "organizationId": "org-broken",
        "company": "Broken",
        "website": "https://this-domain-should-not-exist-123456789.ch"
      },
      {
        "organizationId": "org-empty",
        "company": "No Website",
        "website": ""
      }
    ]
  }')"

echo "$CHECK_RESPONSE" | jq .

COUNT="$(echo "$CHECK_RESPONSE" | jq 'length')"
WORKING_STATUS="$(echo "$CHECK_RESPONSE" | jq -r '.[] | select(.organizationId=="org-working") | .websiteStatus')"
BROKEN_STATUS="$(echo "$CHECK_RESPONSE" | jq -r '.[] | select(.organizationId=="org-broken") | .websiteStatus')"
EMPTY_STATUS="$(echo "$CHECK_RESPONSE" | jq -r '.[] | select(.organizationId=="org-empty") | .websiteStatus')"

if [[ "$COUNT" -ne 3 ]]; then
  echo "Expected 3 results, got: $COUNT"
  exit 1
fi

if [[ "$WORKING_STATUS" != "working" ]]; then
  echo "Expected org-working status working, got: $WORKING_STATUS"
  exit 1
fi

if [[ "$BROKEN_STATUS" != "not-working" ]]; then
  echo "Expected org-broken status not-working, got: $BROKEN_STATUS"
  exit 1
fi

if [[ "$EMPTY_STATUS" != "unknown" ]]; then
  echo "Expected org-empty status unknown, got: $EMPTY_STATUS"
  exit 1
fi

echo "Website status check through bridge passed"

echo
echo "3) Empty request validation"

HTTP_STATUS="$(curl -sS -o /tmp/website-status-empty-response.json -w "%{http_code}" \
  -X POST "$BASE_URL/api/website-status/check" \
  -H "Content-Type: application/json" \
  -d '{"organizations": []}')"

cat /tmp/website-status-empty-response.json | jq .

if [[ "$HTTP_STATUS" != "400" ]]; then
  echo "Expected HTTP 400 for empty request, got: $HTTP_STATUS"
  exit 1
fi

echo "Empty request validation passed"

echo
echo "All bridge smoke tests passed"
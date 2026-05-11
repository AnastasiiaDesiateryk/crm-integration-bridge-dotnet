# CRM Integration Bridge (.NET)

SOA-style .NET bridge service connecting a CRM frontend with an external Python/FastAPI enrichment API.

The service currently supports website availability checks for CRM organizations. It receives organization website data from the CRM, delegates the check to the enrichment API, and returns live status results to the frontend.

## Architecture

```txt
CRM Frontend
   ↓
CRM Integration Bridge (.NET)
   ↓
Python Enricher API
   ↓
Company websites
````

## This bridge provides

The CRM should not be directly coupled to the Python enrichment service.

This bridge provides:

* a stable CRM-facing API contract
* isolation between CRM and enrichment tooling
* upstream wake-up handling for Render-hosted services
* support for future enrichment workflows

Website status is treated as temporary technical state. It is returned to the frontend for visual display only and should not be persisted in the CRM database.

## Endpoints

### Health check

```http
GET /health
```

### Website status check

```http
POST /api/website-status/check
```

Example request:

```json
{
  "organizations": [
    {
      "organizationId": "org-1",
      "company": "Google",
      "website": "https://google.com"
    }
  ]
}
```

Example response:

```json
[
  {
    "organizationId": "org-1",
    "company": "Google",
    "website": "https://google.com",
    "websiteStatus": "working",
    "checkedAt": "2026-05-11T08:57:07Z",
    "error": null
  }
]
```

## Configuration

```env
ENRICHER_API_BASE_URL=https://enricherdescriptionssc-api.onrender.com
```

## Run locally

```bash
dotnet restore
dotnet run
```

Default local URL:

```txt
http://localhost:5000
```

## Smoke test

```bash
./scripts/smoke-test.sh
```

For deployed bridge:

```bash
BASE_URL=https://your-bridge-url.onrender.com ./scripts/smoke-test.sh
```


# ItWorksOnMyResume

Skill exchange marketplace — trade what you know for what you need, locally.

## Stack

| Layer    | Tech                              |
|----------|-----------------------------------|
| Mobile   | React Native (Expo)               |
| Backend  | ASP.NET Core 8 · C#               |
| Database | PostgreSQL 16 + PostGIS           |
| Cache    | Redis 7                           |
| Storage  | MinIO (S3-compatible)             |
| Push     | ntfy (self-hosted)                |
| Logs     | Seq                               |
| Jobs     | Hangfire                          |

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker + Docker Compose

## Quick start
```bash
# Start all services
cd infra && docker compose up -d

# Run backend
cd backend && dotnet run --project src/IWOMR.API

# Run mobile (needs Expo Go on your phone)
cd mobile && npx expo start
```

## Structure
```
ItWorksOnMyResume/
├── backend/    ASP.NET Core API
├── mobile/     React Native app
└── infra/      Docker Compose + config
```

## Branches

- `main` — stable
- `develop` — integration
- `feature/*` — one feature per branch

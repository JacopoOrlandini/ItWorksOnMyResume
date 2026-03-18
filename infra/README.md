# ItWorksOnMyResume — Infra

All backend services run as Docker containers. Zero cloud dependencies.

## Services

| Service    | Image                    | Port(s)        | Purpose                        |
|------------|--------------------------|----------------|--------------------------------|
| PostgreSQL  | postgis/postgis:16-3.4   | 5432           | Main database + geo queries    |
| Redis       | redis:7-alpine           | 6379           | Cache + SignalR backplane      |
| MinIO       | minio/minio              | 9000 / 9001    | Object storage (avatars, docs) |
| ntfy        | binwiederhier/ntfy       | 8090           | Push notifications             |
| Seq         | datalust/seq             | 5341           | Structured log viewer          |
| Nginx       | nginx:alpine             | 80 / 443       | Reverse proxy (prod only)      |

## Prerequisites

- Docker Desktop (Mac/Windows) or Docker Engine + Compose plugin (Linux)
- Minimum 2 GB RAM available to Docker

## First-time setup

```bash
# 1. Create your .env from the template
cp .env.example .env
# Edit .env and set all passwords

# 2. Make the MinIO setup script executable
chmod +x minio/setup.sh

# 3. Start all services (development mode — no Nginx)
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# 4. Verify everything is healthy
docker compose ps
```

## Daily usage

```bash
# Start
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Stop (keeps data)
docker compose down

# Stop and wipe all data (full reset)
docker compose down -v

# View logs
docker compose logs -f postgres
docker compose logs -f redis

# Restart one service
docker compose restart postgres
```

## Service UIs

| Service | URL                    | Credentials          |
|---------|------------------------|----------------------|
| MinIO console | http://localhost:9001 | from your .env |
| Seq log viewer | http://localhost:5341 | no auth in dev |
| ntfy | http://localhost:8090 | admin from .env |

## Connecting from the backend

Use these connection strings in `backend/.env`:

```env
ConnectionStrings__Default=Host=localhost;Port=5432;Database=iwomr;Username=iwomr_app;Password=YOUR_PG_PASSWORD
ConnectionStrings__Redis=localhost:6379,password=YOUR_REDIS_PASSWORD
MinIO__Endpoint=localhost:9000
MinIO__AccessKey=YOUR_MINIO_USER
MinIO__SecretKey=YOUR_MINIO_PASSWORD
Ntfy__BaseUrl=http://localhost:8090
Seq__Url=http://localhost:5341
```

## Connecting from the mobile app (physical device)

The app runs on your phone — `localhost` won't work. Use your machine's LAN IP:

```bash
# Find your IP
ip addr show       # Linux
ipconfig           # Windows
ifconfig en0       # Mac
```

Then in `mobile/.env`:
```env
EXPO_PUBLIC_API_URL=http://192.168.x.x:5000
EXPO_PUBLIC_NTFY_URL=http://192.168.x.x:8090
```

Make sure your firewall allows inbound connections on ports 5000 and 8090.

## Database access (dev)

```bash
# psql directly
docker exec -it iwomr-postgres psql -U iwomr_app -d iwomr

# Or connect with any GUI (TablePlus, DBeaver, DataGrip):
# Host: localhost  Port: 5432  DB: iwomr  User: iwomr_app
```

## Reset the database only

```bash
docker compose stop postgres
docker volume rm infra_pgdata
docker compose start postgres
# init.sql will re-run automatically
```

## Production mode (with Nginx)

```bash
# Place SSL certs in nginx/certs/cert.pem and nginx/certs/key.pem
# Uncomment the HTTPS block in nginx/nginx.conf
docker compose up -d   # base compose only — includes nginx
```

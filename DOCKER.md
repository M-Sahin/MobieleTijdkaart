# Docker Deployment Instructies

## Vereisten
- Docker Desktop geïnstalleerd
- Docker Compose (meestal inbegrepen bij Docker Desktop)

## Database Keuze

De applicatie ondersteunt zowel **SQL Server** als **PostgreSQL**:
- Standaard: SQL Server (LocalDB voor lokale ontwikkeling)
- Docker: PostgreSQL (lichter en sneller)

## Optie 1: Alleen API met Dockerfile

### Build de Docker image
```powershell
docker build -t mobiele-tijdkaart-api .
```

### Run de container
```powershell
docker run -d -p 5179:8080 `
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=MobieleTijdkaartDb;Trusted_Connection=true;TrustServerCertificate=true" `
  --name mobiele-tijdkaart-api `
  mobiele-tijdkaart-api
```

### Bekijk logs
```powershell
docker logs -f mobiele-tijdkaart-api
```

### Stop de container
```powershell
docker stop mobiele-tijdkaart-api
docker rm mobiele-tijdkaart-api
```

## Optie 2: API + PostgreSQL met Docker Compose (Aanbevolen)

### Setup
1. Kopieer `.env.example` naar `.env`:
```powershell
Copy-Item .env.example .env
```

2. Bewerk `.env` en vul je database wachtwoord in:
```
DB_PASSWORD=jouw_veilig_wachtwoord
```

### Start alle services
```powershell
docker-compose up -d
```

Dit start:
- PostgreSQL database op poort 5432
- API applicatie op poort 5179

### Bekijk de status
```powershell
docker-compose ps
```

### Bekijk logs
```powershell
# Alle services
docker-compose logs -f

# Alleen API
docker-compose logs -f api

# Alleen Database
docker-compose logs -f postgres
```

### Stop alle services
```powershell
docker-compose down
```

### Stop en verwijder volumes (LET OP: verwijdert database data!)
```powershell
docker-compose down -v
```

## Database Migraties in Docker

### Voer migraties uit
```powershell
# Als de container draait
docker exec -it mobiele-tijdkaart-api dotnet ef database update
```

Of gebruik de API zelf - de `DbInitializer` voert automatisch migraties uit bij startup.

## API Testen

De API is beschikbaar op:
- **Swagger UI**: http://localhost:5179/swagger
- **API endpoints**: http://localhost:5179/api/

## Omgevingsvariabelen

Pas de volgende variabelen aan in `docker-compose.yml` voor productie:

```yaml
environment:
  - POSTGRES_PASSWORD=JouwVeiligWachtwoord123!  # PostgreSQL wachtwoord
  - ConnectionStrings__PostgresConnection=Host=...  # Connection string
```

⚠️ **Belangrijk**: Verander het POSTGRES_PASSWORD in productie!

## Lokale Ontwikkeling met PostgreSQL

Om PostgreSQL lokaal te gebruiken zonder Docker:

1. Installeer PostgreSQL lokaal
2. Pas `appsettings.json` aan:
```json
{
  "UsePostgreSQL": true,
  "ConnectionStrings": {
    "PostgresConnection": "Host=127.0.0.1;Database=postgres;Username=postgres;Password=pYY3_u.6>cM&8CV"
  }
}
```
3. Voer migraties uit:
```powershell
cd MobieleTijdkaart.Api
dotnet ef database update --project ..\MobieleTijdkaart.Infrastructure
```

## Troubleshooting

### API start niet
```powershell
docker-compose logs api
```

### Database connectie problemen
```powershell
# Test PostgreSQL connectie
docker exec -it mobiele-tijdkaart-db psql -U postgres -d MobieleTijdkaartDb
```

### Reset alles
```powershell
docker-compose down -v
docker-compose up -d --build
```

## Productie Deployment

Voor productie deployment:
1. Gebruik een externe PostgreSQL database of cloud provider (bijv. Supabase, Neon, Railway)
2. Configureer de connection string via environment variables
3. Gebruik Docker secrets voor gevoelige data
4. Configureer HTTPS met SSL certificaten
5. Pas CORS origins aan voor je productie domain
6. Zet `UsePostgreSQL=true` in de environment variables

# Docker Deployment Instructies

## Vereisten
- Docker Desktop geïnstalleerd
- Docker Compose (meestal inbegrepen bij Docker Desktop)

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

## Optie 2: API + SQL Server met Docker Compose (Aanbevolen)

### Start alle services
```powershell
docker-compose up -d
```

Dit start:
- SQL Server database op poort 1433
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
docker-compose logs -f sqlserver
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
  - SA_PASSWORD=JouwVeiligWachtwoord123!  # SQL Server wachtwoord
  - ConnectionStrings__DefaultConnection=Server=...  # Connection string
```

⚠️ **Belangrijk**: Verander het SA_PASSWORD in productie!

## Troubleshooting

### API start niet
```powershell
docker-compose logs api
```

### Database connectie problemen
```powershell
# Test database connectie
docker exec -it mobiele-tijdkaart-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd
```

### Reset alles
```powershell
docker-compose down -v
docker-compose up -d --build
```

## Productie Deployment

Voor productie deployment:
1. Gebruik een externe SQL Server of Azure SQL Database
2. Configureer de connection string via environment variables
3. Gebruik Docker secrets voor gevoelige data
4. Configureer HTTPS met SSL certificaten
5. Pas CORS origins aan voor je productie domain

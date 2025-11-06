# Gebruik de officiële .NET 9 SDK image voor de build fase
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopieer de solution file en alle project files
COPY ["MobieleTijdkaart.sln", "./"]
COPY ["MobieleTijdkaart.Api/MobieleTijdkaart.Api.csproj", "MobieleTijdkaart.Api/"]
COPY ["MobieleTijdkaart.Application/MobieleTijdkaart.Application.csproj", "MobieleTijdkaart.Application/"]
COPY ["MobieleTijdkaart.Domain/MobieleTijdkaart.Domain.csproj", "MobieleTijdkaart.Domain/"]
COPY ["MobieleTijdkaart.Infrastructure/MobieleTijdkaart.Infrastructure.csproj", "MobieleTijdkaart.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MobieleTijdkaart.Api/MobieleTijdkaart.Api.csproj"

# Kopieer de rest van de source code
COPY . .

# Build de applicatie
WORKDIR "/src/MobieleTijdkaart.Api"
RUN dotnet build "MobieleTijdkaart.Api.csproj" -c Release -o /app/build

# Publish de applicatie
FROM build AS publish
RUN dotnet publish "MobieleTijdkaart.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Gebruik de .NET 9 ASP.NET runtime image voor de finale image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Kopieer de gepubliceerde applicatie
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start de applicatie
ENTRYPOINT ["dotnet", "MobieleTijdkaart.Api.dll"]

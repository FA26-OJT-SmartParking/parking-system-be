# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY gateway/Gateway/Gateway.csproj gateway/Gateway/
RUN dotnet restore gateway/Gateway

COPY shared/ shared/
COPY gateway/Gateway/ gateway/Gateway/
RUN dotnet publish gateway/Gateway -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.dll"]

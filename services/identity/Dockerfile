# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY contracts/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj contracts/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY services/identity/Identity.Api/Identity.Api.csproj services/identity/Identity.Api/
RUN dotnet restore services/identity/Identity.Api

COPY contracts/ contracts/
COPY shared/ shared/
COPY services/identity/Identity.Api/ services/identity/Identity.Api/
RUN dotnet publish services/identity/Identity.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]

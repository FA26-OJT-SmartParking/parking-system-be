# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj shared/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY grpc_proto/ParkingSystem.Grpc.csproj grpc_proto/
COPY services/identity/Identity.Domain/Identity.Domain.csproj services/identity/Identity.Domain/
COPY services/identity/Identity.Application/Identity.Application.csproj services/identity/Identity.Application/
COPY services/identity/Identity.Infrastructure/Identity.Infrastructure.csproj services/identity/Identity.Infrastructure/
COPY services/identity/Identity.Persistence/Identity.Persistence.csproj services/identity/Identity.Persistence/
COPY services/identity/Identity.WebAPI/Identity.WebAPI.csproj services/identity/Identity.WebAPI/
RUN dotnet restore services/identity/Identity.WebAPI

COPY shared/ shared/
COPY grpc_proto/ grpc_proto/
COPY services/identity/ services/identity/
RUN dotnet publish services/identity/Identity.WebAPI -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.WebAPI.dll"]

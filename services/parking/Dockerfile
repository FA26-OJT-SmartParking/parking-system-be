# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj shared/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY grpc_proto/ParkingSystem.Grpc.csproj grpc_proto/
COPY services/parking/Parking.Domain/Parking.Domain.csproj services/parking/Parking.Domain/
COPY services/parking/Parking.Application/Parking.Application.csproj services/parking/Parking.Application/
COPY services/parking/Parking.Infrastructure/Parking.Infrastructure.csproj services/parking/Parking.Infrastructure/
COPY services/parking/Parking.Persistence/Parking.Persistence.csproj services/parking/Parking.Persistence/
COPY services/parking/Parking.WebAPI/Parking.WebAPI.csproj services/parking/Parking.WebAPI/
RUN dotnet restore services/parking/Parking.WebAPI

COPY shared/ shared/
COPY grpc_proto/ grpc_proto/
COPY services/parking/ services/parking/
RUN dotnet publish services/parking/Parking.WebAPI -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "Parking.WebAPI.dll"]

# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj shared/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY grpc_proto/ParkingSystem.Grpc.csproj grpc_proto/
COPY services/notification/Notification.Domain/Notification.Domain.csproj services/notification/Notification.Domain/
COPY services/notification/Notification.Application/Notification.Application.csproj services/notification/Notification.Application/
COPY services/notification/Notification.Infrastructure/Notification.Infrastructure.csproj services/notification/Notification.Infrastructure/
COPY services/notification/Notification.API/Notification.API.csproj services/notification/Notification.API/
RUN dotnet restore services/notification/Notification.API

COPY shared/ shared/
COPY grpc_proto/ grpc_proto/
COPY services/notification/ services/notification/
RUN dotnet publish services/notification/Notification.API -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notification.API.dll"]

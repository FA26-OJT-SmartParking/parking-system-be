# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj shared/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY grpc_proto/ParkingSystem.Grpc.csproj grpc_proto/
COPY services/booking/Booking.Domain/Booking.Domain.csproj services/booking/Booking.Domain/
COPY services/booking/Booking.Application/Booking.Application.csproj services/booking/Booking.Application/
COPY services/booking/Booking.Infrastructure/Booking.Infrastructure.csproj services/booking/Booking.Infrastructure/
COPY services/booking/Booking.Persistence/Booking.Persistence.csproj services/booking/Booking.Persistence/
COPY services/booking/Booking.WebAPI/Booking.WebAPI.csproj services/booking/Booking.WebAPI/
RUN dotnet restore services/booking/Booking.WebAPI

COPY shared/ shared/
COPY grpc_proto/ grpc_proto/
COPY services/booking/ services/booking/
RUN dotnet publish services/booking/Booking.WebAPI -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Booking.WebAPI.dll"]

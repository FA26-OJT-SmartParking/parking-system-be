# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY contracts/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj contracts/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY services/booking/Booking.Api/Booking.Api.csproj services/booking/Booking.Api/
RUN dotnet restore services/booking/Booking.Api

COPY contracts/ contracts/
COPY shared/ shared/
COPY services/booking/Booking.Api/ services/booking/Booking.Api/
RUN dotnet publish services/booking/Booking.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Booking.Api.dll"]

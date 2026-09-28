# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY contracts/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj contracts/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY services/payment/Payment.Api/Payment.Api.csproj services/payment/Payment.Api/
RUN dotnet restore services/payment/Payment.Api

COPY contracts/ contracts/
COPY shared/ shared/
COPY services/payment/Payment.Api/ services/payment/Payment.Api/
RUN dotnet publish services/payment/Payment.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]

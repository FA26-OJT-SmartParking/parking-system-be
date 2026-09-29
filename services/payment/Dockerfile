# Build context: repository root (see deploy/docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY shared/ParkingSystem.Contracts/ParkingSystem.Contracts.csproj shared/ParkingSystem.Contracts/
COPY shared/ParkingSystem.ServiceDefaults/ParkingSystem.ServiceDefaults.csproj shared/ParkingSystem.ServiceDefaults/
COPY grpc_proto/ParkingSystem.Grpc.csproj grpc_proto/
COPY services/payment/Payment.Domain/Payment.Domain.csproj services/payment/Payment.Domain/
COPY services/payment/Payment.Application/Payment.Application.csproj services/payment/Payment.Application/
COPY services/payment/Payment.Infrastructure/Payment.Infrastructure.csproj services/payment/Payment.Infrastructure/
COPY services/payment/Payment.WebAPI/Payment.WebAPI.csproj services/payment/Payment.WebAPI/
RUN dotnet restore services/payment/Payment.WebAPI

COPY shared/ shared/
COPY grpc_proto/ grpc_proto/
COPY services/payment/ services/payment/
RUN dotnet publish services/payment/Payment.WebAPI -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.WebAPI.dll"]

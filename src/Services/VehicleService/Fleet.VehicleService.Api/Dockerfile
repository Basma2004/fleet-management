# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy Solution & Directory.Build.props
COPY Directory.Build.props ./
COPY FleetManagement.sln ./

# Copy Project Files for Caching Restore Layers
COPY src/BuildingBlocks/Fleet.Shared.Domain/*.csproj ./src/BuildingBlocks/Fleet.Shared.Domain/
COPY src/BuildingBlocks/Fleet.Shared.Contracts/*.csproj ./src/BuildingBlocks/Fleet.Shared.Contracts/
COPY src/BuildingBlocks/Fleet.Shared.Messaging/*.csproj ./src/BuildingBlocks/Fleet.Shared.Messaging/
COPY src/BuildingBlocks/Fleet.Shared.Auth/*.csproj ./src/BuildingBlocks/Fleet.Shared.Auth/
COPY src/BuildingBlocks/Fleet.Shared.Observability/*.csproj ./src/BuildingBlocks/Fleet.Shared.Observability/

COPY src/Services/VehicleService/Fleet.VehicleService.Domain/*.csproj ./src/Services/VehicleService/Fleet.VehicleService.Domain/
COPY src/Services/VehicleService/Fleet.VehicleService.Application/*.csproj ./src/Services/VehicleService/Fleet.VehicleService.Application/
COPY src/Services/VehicleService/Fleet.VehicleService.Infrastructure/*.csproj ./src/Services/VehicleService/Fleet.VehicleService.Infrastructure/
COPY src/Services/VehicleService/Fleet.VehicleService.Api/*.csproj ./src/Services/VehicleService/Fleet.VehicleService.Api/

RUN dotnet restore src/Services/VehicleService/Fleet.VehicleService.Api/Fleet.VehicleService.Api.csproj

# Copy all source files and publish
COPY src/ ./src/
RUN dotnet publish src/Services/VehicleService/Fleet.VehicleService.Api/Fleet.VehicleService.Api.csproj -c Release -o /app/publish --no-restore

# Runtime Stage (Lightweight ASP.NET 9 container)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fleet.VehicleService.Api.dll"]
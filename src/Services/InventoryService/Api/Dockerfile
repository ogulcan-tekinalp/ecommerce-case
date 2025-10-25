# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/InventoryService/Api/InventoryService.Api.csproj", "src/Services/InventoryService/Api/"]
COPY ["src/Services/InventoryService/Application/InventoryService.Application.csproj", "src/Services/InventoryService/Application/"]
COPY ["src/Services/InventoryService/Domain/InventoryService.Domain.csproj", "src/Services/InventoryService/Domain/"]
COPY ["src/Services/InventoryService/Infrastructure/InventoryService.Infrastructure.csproj", "src/Services/InventoryService/Infrastructure/"]
COPY ["src/BuildingBlocks/Persistence/BuildingBlocks.Persistence.csproj", "src/BuildingBlocks/Persistence/"]
COPY ["src/BuildingBlocks/Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/Messaging/"]
COPY ["src/BuildingBlocks/Observability/BuildingBlocks.Observability.csproj", "src/BuildingBlocks/Observability/"]

# Restore dependencies
RUN dotnet restore "src/Services/InventoryService/Api/InventoryService.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/InventoryService/Api"
RUN dotnet build "InventoryService.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "InventoryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]

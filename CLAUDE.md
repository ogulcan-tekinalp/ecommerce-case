# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a microservices-based e-commerce platform built with .NET 9 implementing the Saga pattern for distributed transactions. The system demonstrates event-driven architecture with compensating transactions across Order, Inventory, and Payment services.

## Architecture

### Services
- **OrderService**: Manages order lifecycle and orchestrates the order saga
- **InventoryService**: Handles stock reservations and availability checks
- **PaymentService**: Processes payments

### BuildingBlocks
Shared libraries used across all services:
- **Messaging**: RabbitMQ-based event bus (IMessageBus) and integration events
- **Persistence**: Common database utilities
- **Observability**: Logging and monitoring infrastructure

### Key Patterns
1. **Saga Pattern**: OrderSaga orchestrates the distributed transaction flow (OrderService.Application.Sagas.OrderSaga)
2. **CQRS with MediatR**: Commands and queries separated using MediatR handlers
3. **Event-Driven**: Services communicate via RabbitMQ integration events
4. **Compensating Transactions**: Automatic rollback on failures (e.g., stock release on payment failure)

### Flow
1. Order created → OrderCreatedEvent published
2. InventoryService reserves stock → StockReservedEvent published
3. PaymentService processes payment → PaymentProcessedEvent published
4. OrderSaga confirms order → OrderConfirmedEvent published
5. On failure: OrderSaga triggers compensation (StockReleasedEvent, OrderCancelledEvent)

### Important Implementation Details
- **Idempotency**: Orders support idempotency keys to prevent duplicates
- **Stock Reservation**: Auto-cleanup after 10 minutes (StockReservationCleanupService background job)
- **VIP Priority**: Orders can be marked as VIP for priority processing
- **Business Rules**: Orders can only be cancelled within 2 hours of creation

## Development Commands

### Build & Restore
```bash
dotnet restore
dotnet build
```

### Run Services
Each service must be run separately:
```bash
# Terminal 1 - Order Service
dotnet run --project src/Services/OrderService/Api/OrderService.Api.csproj

# Terminal 2 - Inventory Service
dotnet run --project src/Services/InventoryService/Api/InventoryService.Api.csproj

# Terminal 3 - Payment Service
dotnet run --project src/Services/PaymentService/Api/PaymentService.Api.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~CreateOrderCommandHandlerTests"
```

### Infrastructure
Start dependencies with Docker Compose:
```bash
docker-compose up -d
```

This starts:
- PostgreSQL (port 5432) - credentials: app/app
- RabbitMQ (port 5672, management UI: 15672) - credentials: guest/guest

## Database

All services use PostgreSQL with EF Core. Each service has its own DbContext:
- OrderDbContext (OrderService)
- InventoryDbContext (InventoryService)
- PaymentService uses simpler storage

### Migrations
When changing entities, create and apply migrations:
```bash
# From service Api project directory
dotnet ef migrations add MigrationName --project ../Infrastructure

# Apply migrations
dotnet ef database update --project ../Infrastructure
```

## Configuration

Connection strings are in `appsettings.json` for each service:
- **Default**: PostgreSQL connection
- **RabbitMQ**: Message bus connection

Services communicate via:
- RabbitMQ events (primary)
- HTTP endpoints (available but not primary flow)

## Testing

Tests use:
- xUnit for test framework
- FluentAssertions for assertions
- Moq for mocking
- Bogus for test data generation
- InMemory EF Core for database tests

## Service Ports (Default Development)

Check launchSettings.json in each Api project for actual ports, typically:
- OrderService: 5XXX
- InventoryService: 5207 (referenced in OrderService appsettings.json)
- PaymentService: 5XXX
- RabbitMQ Management UI: http://localhost:15672

## Logging

All services use Serilog with structured logging:
- Console output
- File output: `logs/{service}-.txt` (rolling daily)
- Each log entry enriched with Service property

## Key Files

- `src/Services/OrderService/Application/Sagas/OrderSaga.cs` - Main saga orchestration
- `src/BuildingBlocks/Messaging/Events/` - All integration events
- `src/BuildingBlocks/Messaging/IMessageBus.cs` - Event bus abstraction
- `src/Services/InventoryService/Application/BackgroundJobs/StockReservationCleanupService.cs` - Auto-release expired reservations
- `docker-compose.yml` - Infrastructure dependencies

## Event Flow Reference

Integration events (in BuildingBlocks.Messaging.Events):
- OrderCreatedEvent → InventoryService
- StockReservedEvent → PaymentService + OrderSaga
- PaymentProcessedEvent → OrderSaga
- PaymentFailedEvent → OrderSaga (triggers compensation)
- StockReleasedEvent → InventoryService (compensation)
- OrderCancelledEvent → published after cancellation
- OrderConfirmedEvent → published on success
- OrderShippedEvent → for shipping flow

## Health Checks

Each service exposes:
- `/health` - Health check endpoint (includes DB and RabbitMQ checks)
- `/openapi` - OpenAPI spec (Development only)

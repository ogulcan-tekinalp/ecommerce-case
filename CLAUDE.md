# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

```bash
# Build entire solution
dotnet build EcommerceCase.sln

# Build specific service
dotnet build src/Services/OrderService/Api/OrderService.Api.csproj
dotnet build src/Services/InventoryService/Api/InventoryService.Api.csproj
dotnet build src/Services/PaymentService/Api/PaymentService.Api.csproj

# Run specific service
dotnet run --project src/Services/OrderService/Api/OrderService.Api.csproj
dotnet run --project src/Services/InventoryService/Api/InventoryService.Api.csproj
dotnet run --project src/Services/PaymentService/Api/PaymentService.Api.csproj

# Database migrations (Entity Framework Core)
# Create migration
dotnet ef migrations add <MigrationName> --project src/Services/OrderService/Infrastructure --startup-project src/Services/OrderService/Api

# Apply migrations
dotnet ef database update --project src/Services/OrderService/Infrastructure --startup-project src/Services/OrderService/Api

# Start infrastructure (PostgreSQL)
docker-compose up -d
```

## Architecture Overview

This is a microservices-based e-commerce system built with .NET 9, implementing a **Saga pattern** for distributed transaction orchestration.

### Project Structure

```
src/
├── BuildingBlocks/          # Shared infrastructure libraries
│   ├── Messaging/           # In-memory message bus for event-driven communication
│   ├── Observability/       # Shared observability primitives
│   └── Persistence/         # Shared persistence abstractions
└── Services/
    ├── OrderService/        # Order management with saga orchestration
    ├── InventoryService/    # Stock reservation and management
    └── PaymentService/      # Payment processing
```

### Service Architecture Pattern

Each service follows **Clean Architecture** with four layers:
- **Api**: ASP.NET Core Web API, controllers, middleware
- **Application**: Use cases (CQRS with MediatR), DTOs, interfaces
- **Domain**: Entities, value objects, domain logic
- **Infrastructure**: EF Core DbContext, repositories, external integrations

### Event-Driven Communication

Services communicate via an **in-memory message bus** (`BuildingBlocks.Messaging`):
- All events inherit from `IntegrationEvent` (abstract record with Id, OccurredOnUtc, CorrelationId)
- Event handlers subscribe via `IMessageBus.Subscribe<TEvent>(Func<TEvent, Task>)`
- Events published via `IMessageBus.PublishAsync<TEvent>()`

Key events:
- `OrderCreatedEvent` → triggers stock reservation
- `StockReservedEvent` → triggers payment processing
- `PaymentProcessedEvent` → confirms order
- `PaymentFailedEvent` / `StockReleasedEvent` → compensation flows

### Saga Pattern Implementation

**OrderSaga** (in OrderService) orchestrates the distributed transaction:

1. **Order Created** → Publishes `OrderCreatedEvent`
2. **Stock Reservation**:
   - InventoryService reserves stock
   - Publishes `StockReservedEvent` (success/failure)
   - On failure: Order cancelled
3. **Payment Processing**:
   - PaymentService processes payment
   - Publishes `PaymentProcessedEvent` (success/failure)
   - On failure: Compensates by releasing stock via `StockReleasedEvent`, then cancels order
4. **Order Confirmed** → Final state if all steps succeed

The saga is registered as singleton in `OrderService.Api.Program.cs:34` and initialized at startup.

### Business Rules

- **Order Cancellation Window**: Orders can only be cancelled within 2 hours of creation (`Order.CanBeCancelled()` in OrderService.Domain/Order.cs:42-49)
- **VIP Orders**: Supported via `IsVip` flag on orders
- **Stock Reservations**: Track reservation IDs for compensation
- **Payment Tracking**: Orders store `PaymentId` on successful payment

### Key Technologies

- **.NET 9**: Target framework across all projects
- **Entity Framework Core 9**: PostgreSQL provider (Npgsql)
- **MediatR v12**: CQRS pattern for commands/queries
- **FluentValidation**: Request validation
- **OpenAPI**: API documentation (dev environment only)

### Database

- **PostgreSQL 16** via docker-compose
- Connection: `localhost:5432`
- Credentials: app/app
- Database: ecommerce

Each service has its own schema/tables managed via EF Core migrations.

### Important Implementation Notes

1. **Scoped Dependencies in Saga**: OrderSaga is singleton but uses `IServiceScopeFactory` to create scopes for accessing scoped repositories
2. **Event Handlers**: Registered as singletons and subscribe to events in constructor
3. **CQRS**: Commands/queries follow vertical slice architecture under `Application/[Feature]/` directories
4. **Error Handling**: Custom middleware `ErrorHandlingMiddleware` in OrderService.Api
5. **Namespace Conventions**:
   - Domain entities: `[ServiceName].Domain.Entities`
   - Events: `BuildingBlocks.Messaging.Events`
   - Application layer: `[ServiceName].Application.[Feature]`

### HTTP Client Configuration

- OrderService uses `IHttpClientFactory` for calling InventoryService
- Client configured in `OrderService.Infrastructure.csproj` with Microsoft.Extensions.Http package

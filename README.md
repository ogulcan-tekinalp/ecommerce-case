# E-Commerce Order Management System

> **Enterprise-grade microservices architecture for order management with event-driven communication**

## Overview

This project implements a comprehensive e-commerce order management system using modern microservices architecture. The system handles order lifecycle, inventory management, and payment processing with robust business rules and enterprise patterns.

## Architecture

### Microservices
- **Order Service** - Order lifecycle management (PENDING → CONFIRMED → SHIPPED → DELIVERED)
- **Inventory Service** - Real-time stock control with optimistic locking
- **Payment Service** - Payment processing with fraud detection and retry mechanisms

### Key Patterns
- **CQRS** with MediatR
- **Event-Driven Architecture** with RabbitMQ/In-Memory Message Bus
- **Saga Pattern** for distributed transactions
- **Repository Pattern** with Unit of Work
- **Optimistic Locking** for concurrency control
- **Idempotency** for duplicate prevention

## Quick Start

### Prerequisites
- .NET 9 SDK
- Docker & Docker Compose
- PostgreSQL, RabbitMQ, Redis (via Docker)

### 1. Clone & Setup
```bash
git clone <repository-url>
cd ecommerce-case
```

### 2. Start Infrastructure
```bash
docker-compose up -d
```

### 3. Run Tests
```bash
# All Unit Tests (41 tests) - All Passing
dotnet test --verbosity minimal

# Individual service tests
dotnet test tests/OrderService.Tests/      # 26 tests
dotnet test tests/InventoryService.Tests/  # 7 tests
dotnet test tests/PaymentService.Tests/    # 8 tests

# Integration & Performance tests
dotnet test tests/IntegrationTests/        # 3 tests (TestContainers)
dotnet test tests/PerformanceTests/        # Load tests (NBomber)
```

### 4. Start Services
```bash
# Terminal 1 - Order Service
cd src/Services/OrderService/Api
dotnet run

# Terminal 2 - Inventory Service  
cd src/Services/InventoryService/Api
dotnet run

# Terminal 3 - Payment Service
cd src/Services/PaymentService/Api
dotnet run
```

### 5. Health Checks
```bash
curl http://localhost:5001/health  # Order Service
curl http://localhost:5002/health  # Inventory Service  
curl http://localhost:5003/health  # Payment Service
```

## API Endpoints

### Order Management
```http
POST   /api/v1/orders                      # Create order
GET    /api/v1/orders/{orderId}           # Get order details
PUT    /api/v1/orders/{orderId}/cancel    # Cancel order
GET    /api/v1/orders/customer/{customerId} # Customer orders
POST   /api/v1/orders/{orderId}/retry     # Retry failed order
GET    /api/v1/orders/vip                 # VIP orders
```

### Inventory Management
```http
POST   /api/v1/inventory/check-availability  # Check stock
POST   /api/v1/inventory/reserve            # Reserve stock
POST   /api/v1/inventory/release            # Release stock
GET    /api/v1/inventory/products/{id}/stock # Get stock info
```

### Payment Processing
```http
POST   /api/v1/payments/process      # Process payment
POST   /api/v1/payments/refund       # Process refund
GET    /api/v1/payments/{paymentId}  # Payment status
POST   /api/v1/payments/validate     # Validate payment method
```

## Testing Examples

### Create Order
```bash
curl -X POST http://localhost:5001/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "isVip": false,
    "items": [{
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "productName": "Laptop",
      "quantity": 1,
      "unitPrice": 15000
    }]
  }'
```

### Check Order Status
```bash
curl http://localhost:5001/api/v1/orders/{orderId}
```

### Check Stock
```bash
curl http://localhost:5002/api/v1/inventory/products/{productId}/stock
```

## Business Rules

### Order Rules
- Min amount: 100 TL, Max amount: 50,000 TL
- Max 20 items per order
- 2-hour cancellation window
- VIP customer priority processing
- Idempotency with duplicate prevention

### Inventory Rules
- 10-minute automatic stock release
- Low stock alerts (< 10 items)
- 50% reservation limit per product
- Flash sale limits (2 items per customer)
- Optimistic locking for race conditions

### Payment Rules
- 3 retry attempts with exponential backoff
- 85% success, 10% timeout, 5% failure simulation
- Fraud detection with rule-based system
- Automatic refunds for cancelled orders

## Technology Stack

### Core Technologies
- **.NET 9** - Latest framework
- **Entity Framework Core 9** - Code-first approach
- **PostgreSQL** - Primary database
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **Polly** - Resilience patterns

### Testing & Quality
- **xUnit** - Unit testing framework
- **FluentAssertions** - Test assertions
- **TestContainers** - Integration testing
- **Bogus** - Test data generation
- **NBomber** - Performance testing

### Observability
- **Serilog** - Structured logging
- **OpenTelemetry** - Distributed tracing
- **Elasticsearch** - Log aggregation
- **Correlation ID** - Request tracking

### Infrastructure & Messaging
- **RabbitMQ** - Event-driven messaging
- **Redis** - Distributed caching
- **Health Checks** - Service monitoring
- **Docker Compose** - Container orchestration

## Performance & Scalability

### Optimizations
- **Optimistic Locking** - Prevents race conditions
- **Connection Pooling** - Database efficiency
- **Async/Await** - Non-blocking operations
- **Background Services** - Automated cleanup
- **Dead Letter Queue** - Message reliability

### Monitoring
- **Health Endpoints** - Service status
- **Structured Logs** - Searchable events
- **Correlation IDs** - Request tracing
- **Performance Metrics** - Load testing ready

## Configuration

### Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=ecommerce;Username=postgres;Password=postgres"

# Logging
Serilog__MinimumLevel="Information"
Serilog__WriteTo__Console__Enabled=true

# Services
Services__OrderService__BaseUrl="http://localhost:5001"
Services__InventoryService__BaseUrl="http://localhost:5002"
Services__PaymentService__BaseUrl="http://localhost:5003"
```

## Docker Support

### Full Stack
```bash
docker-compose up -d
```

### Individual Services
```bash
docker build -t order-service -f src/Services/OrderService/Api/Dockerfile .
docker run -p 5001:8080 order-service
```

## Test Coverage

- **Unit Tests**: 41/41 passing (Core business logic)
  - OrderService: 26 tests (validation, VIP, saga patterns)
  - InventoryService: 7 tests (stock management, reservations)  
  - PaymentService: 8 tests (processing, fraud detection, refunds)
- **Integration Tests**: 3/3 passing (E2E workflows with TestContainers)
- **Performance Tests**: Load testing (NBomber concurrent users)
- **Manual Edge Cases**: 10/10 passing (Validation, limits, timeouts)
- **Payment Simulation**: 85% success, 10% timeout, 5% failure

## Troubleshooting

### Common Issues
1. **Port Conflicts**: Ensure ports 5001-5003 are available
2. **Database Connection**: Verify PostgreSQL is running
3. **Docker Issues**: Run `docker-compose down && docker-compose up -d`

### Logs Location
- **Console**: Real-time structured logs
- **Elasticsearch**: Aggregated logs (if configured)
- **Files**: `logs/` directory in each service

## Additional Resources

- **API Documentation**: Swagger UI available at `/swagger`
- **Health Checks**: Available at `/health` endpoint
- **Metrics**: Prometheus metrics at `/metrics`

---

## System Status: PRODUCTION READY

- **All Tests Passing**: 41 unit + 3 integration + performance tests
- **Payment Simulation**: 85% success, 10% timeout, 5% failure
- **Event-Driven Flow**: Order → Inventory → Payment → Confirmation
- **Edge Cases Covered**: Validation, limits, concurrency, failures
- **Microservices**: 3 services with health checks
- **Docker Ready**: Full containerization support

**Built with .NET 9 and modern enterprise patterns**
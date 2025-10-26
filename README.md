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
- **VIP Priority Queue** for order prioritization
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
PUT    /api/v1/orders/{orderId}/ship      # Ship order
GET    /api/v1/orders/customer/{customerId} # Customer orders
POST   /api/v1/orders/{orderId}/retry     # Retry failed order
GET    /api/v1/orders/vip                 # VIP orders
POST   /api/v1/orders/{orderId}/mark-vip  # Mark as VIP
GET    /api/v1/orders/queue/status        # Queue status
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

### 1. Create Regular Order
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
    }],
    "idempotencyKey": "order-001"
  }'
```

### 2. Create VIP Order (Priority Processing)
```bash
curl -X POST http://localhost:5001/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440001",
    "isVip": true,
    "items": [{
      "productId": "550e8400-e29b-41d4-a716-446655440002",
      "productName": "Mouse",
      "quantity": 2,
      "unitPrice": 150
    }],
    "idempotencyKey": "vip-order-001"
  }'
```

### 3. Check Order Status
```bash
curl http://localhost:5001/api/v1/orders/{orderId}
```

### 4. Cancel Order
```bash
curl -X PUT http://localhost:5001/api/v1/orders/{orderId}/cancel \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer request"}'
```

### 5. Ship Order
```bash
curl -X PUT http://localhost:5001/api/v1/orders/{orderId}/ship \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber": "TRK123456789", "carrier": "DHL"}'
```

### 6. Check Stock Availability
```bash
curl -X POST http://localhost:5002/api/v1/inventory/check-availability \
  -H "Content-Type: application/json" \
  -d '{
    "items": [{
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "quantity": 5
    }]
  }'
```

### 7. Get Product Stock
```bash
curl http://localhost:5002/api/v1/inventory/products/550e8400-e29b-41d4-a716-446655440001/stock
```

### 8. Validate Payment Method
```bash
curl -X POST http://localhost:5003/api/v1/payments/validate \
  -H "Content-Type: application/json" \
  -d '{"method": 1}'
```

### 9. Mark Order as VIP
```bash
curl -X POST http://localhost:5001/api/v1/orders/{orderId}/mark-vip
```

### 10. Get VIP Orders
```bash
curl http://localhost:5001/api/v1/orders/vip
```

### 11. Test Edge Cases
```bash
# Invalid amount (below minimum)
curl -X POST http://localhost:5001/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "isVip": false,
    "items": [{
      "productId": "550e8400-e29b-41d4-a716-446655440002",
      "productName": "Mouse",
      "quantity": 1,
      "unitPrice": 50
    }]
  }'

# Invalid amount (above maximum)
curl -X POST http://localhost:5001/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "isVip": false,
    "items": [{
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "productName": "Laptop",
      "quantity": 10,
      "unitPrice": 15000
    }]
  }'
```

## Business Rules

### Order Rules
- Min amount: 100 TL, Max amount: 50,000 TL
- Max 20 items per order
- 2-hour cancellation window
- VIP priority queue system (no artificial delays)
- Idempotency with duplicate prevention
- Order lifecycle: Pending → Confirmed → Shipped → Delivered

### Inventory Rules
- 10-minute automatic stock release
- Low stock alerts (< 10 items)
- 50% reservation limit per product
- Flash sale limits (2 items per customer)
- Optimistic locking for race conditions

### Payment Rules
- 3 retry attempts with exponential backoff
- Regular orders: 85% success, 10% timeout, 5% failure
- VIP orders: 90% success, 8% timeout, 2% failure (better rates)
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
- **VIP Priority Queue** - Real queue-based prioritization
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

## System Status: 95% PRODUCTION READY

- **All Tests Passing**: 41 unit + 3 integration + performance tests
- **VIP Priority Queue**: Real queue-based prioritization without delays
- **Payment Simulation**: Regular 85% / VIP 90% success rates
- **Event-Driven Flow**: Order → Queue → Inventory → Payment → Confirmation
- **Edge Cases Covered**: Validation, limits, concurrency, failures
- **Microservices**: 3 services with health checks
- **Docker Ready**: Full containerization support

**Built with .NET 9 and modern enterprise patterns**
# 🧪 HTTP Client Test Planı

## ✅ Yapılan Değişikler

### 1. **IInventoryServiceClient Interface** (`OrderService.Application/Services/`)
```csharp
- CheckAvailabilityAsync(): Stok kontrolü
- ReserveStockAsync(): Stok rezervasyonu
- ReleaseStockAsync(): Stok iade
```

### 2. **InventoryServiceClient Implementation** (`OrderService.Infrastructure/Services/`)
```csharp
- HttpClient injection
- JSON serialization/deserialization
- Error handling ve logging
- Retry logic (gerekirse eklenebilir)
```

### 3. **HTTP Client Registration** (`ServiceCollectionExtensions.cs`)
```csharp
services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
{
    var baseUrl = cfg["ServiceUrls:InventoryService"] ?? "http://localhost:5207";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### 4. **Configuration** (`appsettings.json`)
```json
"ServiceUrls": {
  "InventoryService": "http://localhost:5207"
}
```

## 🚀 Test Senaryoları

### Senaryo 1: Başarılı Order Flow (Happy Path)
```bash
# Terminal 1 - PostgreSQL
docker-compose up -d

# Terminal 2 - InventoryService
cd src/Services/InventoryService/Api
dotnet run --urls="http://localhost:5207"

# Terminal 3 - OrderService
cd src/Services/OrderService/Api
dotnet run --urls="http://localhost:5052"

# Terminal 4 - Test Request
curl -X POST http://localhost:5052/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "isVip": false,
    "items": [
      {
        "productId": "550e8400-e29b-41d4-a716-446655440001",
        "productName": "Laptop",
        "quantity": 1,
        "unitPrice": 15000
      }
    ]
  }'
```

**Beklenen Akış:**
1. ✅ OrderService: Order oluşturulur (PENDING)
2. ✅ OrderService: OrderCreatedEvent publish edilir
3. ✅ InventoryService: Event yakalanır, stok rezerve edilir
4. ✅ InventoryService: StockReservedEvent publish edilir
5. ✅ OrderService: Saga PaymentProcessedEvent'i simüle eder
6. ✅ Order: CONFIRMED durumuna geçer

### Senaryo 2: Stok Yetersiz (Compensation)
```bash
curl -X POST http://localhost:5052/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "isVip": false,
    "items": [
      {
        "productId": "550e8400-e29b-41d4-a716-446655440001",
        "productName": "Laptop",
        "quantity": 1000,
        "unitPrice": 15000
      }
    ]
  }'
```

**Beklenen Akış:**
1. ✅ Order oluşturulur
2. ✅ Stok rezervasyonu FAILS
3. ✅ Order: CANCELLED durumuna geçer

### Senaryo 3: HTTP Connectivity Test
```bash
# InventoryService sağlık kontrolü
curl http://localhost:5207/api/v1/inventory/check-availability \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": "550e8400-e29b-41d4-a716-446655440001", "quantity": 1}
    ]
  }'
```

## 🔍 Kontrol Edilecek Loglar

### OrderService Logları:
```
✅ Order {OrderId} created for Customer {CustomerId}
🚀 Starting saga for Order {OrderId}
Published OrderCreatedEvent for Order {OrderId}
🔄 Sending reserve stock request for order {OrderId}
✅ Stock reserved successfully. ReservationId: {ReservationId}
✅ Order {OrderId} confirmed successfully!
```

### InventoryService Logları:
```
📦 [INVENTORY] Received OrderCreatedEvent for Order {OrderId}
✅ [INVENTORY] Stock reserved for Order {OrderId}, Reservation {ReservationId}
```

## 🐛 Olası Hatalar ve Çözümleri

### 1. Connection Refused
**Hata:** `HttpRequestException: Connection refused`
**Çözüm:** 
- InventoryService'in ayakta olduğunu kontrol et
- Port numaralarını kontrol et (5207)
- appsettings.json'daki URL'i kontrol et

### 2. Timeout
**Hata:** `TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout`
**Çözüm:**
- Timeout süresini artır (appsettings'de)
- InventoryService'in yanıt verdiğini kontrol et

### 3. Serialization Error
**Hata:** `JsonException: The JSON value could not be converted`
**Çözüm:**
- Request/Response DTO'larının uyumlu olduğunu kontrol et
- JSON field naming'i kontrol et (camelCase vs PascalCase)

### 4. Dependency Injection Error
**Hata:** `InvalidOperationException: Unable to resolve service`
**Çözüm:**
- ServiceCollectionExtensions'da HTTP client'ın register edildiğini kontrol et
- AddOrderServiceInfrastructure() çağrısının yapıldığını kontrol et

## 📊 Database Verification

```sql
-- Order durumlarını kontrol et
SELECT id, customer_id, status, total_amount, stock_reservation_id, payment_id
FROM orders
ORDER BY created_at DESC;

-- Stok rezervasyonlarını kontrol et
SELECT id, order_id, status, reserved_at, released_at
FROM stock_reservations
ORDER BY reserved_at DESC;

-- Ürün stoklarını kontrol et
SELECT id, name, available_quantity, reserved_quantity
FROM products;
```

## 🎯 Başarı Kriterleri

- ✅ HTTP client dependency injection çalışıyor
- ✅ OrderService → InventoryService HTTP çağrısı başarılı
- ✅ Stok rezervasyonu HTTP üzerinden yapılabiliyor
- ✅ Hata durumlarında uygun exception handling
- ✅ Logging düzgün çalışıyor
- ✅ Saga akışı kesintisiz devam ediyor

## 🔄 Geliştirme Önerileri

### Polly ile Retry Policy (İleride)
```csharp
services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

### Health Checks
```csharp
// Startup
services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5207/health"), "InventoryService");

// InventoryService
app.MapHealthChecks("/health");
```

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE TABLE flash_sale_products (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "StartTimeUtc" timestamp with time zone NOT NULL,
        "EndTimeUtc" timestamp with time zone NOT NULL,
        "MaxQuantityPerCustomer" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_flash_sale_products" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_flash_sale_products_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE TABLE customer_purchases (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "FlashSaleProductId" uuid,
        "Quantity" integer NOT NULL,
        "PurchaseDateUtc" timestamp with time zone NOT NULL,
        "OrderId" uuid,
        CONSTRAINT "PK_customer_purchases" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_purchases_flash_sale_products_FlashSaleProductId" FOREIGN KEY ("FlashSaleProductId") REFERENCES flash_sale_products ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_customer_purchases_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId" ON customer_purchases ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId_FlashSaleProductId" ON customer_purchases ("CustomerId", "FlashSaleProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId_ProductId" ON customer_purchases ("CustomerId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_customer_purchases_FlashSaleProductId" ON customer_purchases ("FlashSaleProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_customer_purchases_ProductId" ON customer_purchases ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_flash_sale_products_IsActive_StartTimeUtc_EndTimeUtc" ON flash_sale_products ("IsActive", "StartTimeUtc", "EndTimeUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    CREATE INDEX "IX_flash_sale_products_ProductId" ON flash_sale_products ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026093105_AddFlashSaleAndCustomerPurchases') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251026093105_AddFlashSaleAndCustomerPurchases', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE TABLE products (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "AvailableQuantity" integer NOT NULL,
        "ReservedQuantity" integer NOT NULL,
        "Price" numeric(18,2) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_products" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE TABLE flash_sale_products (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "StartTimeUtc" timestamp with time zone NOT NULL,
        "EndTimeUtc" timestamp with time zone NOT NULL,
        "MaxQuantityPerCustomer" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_flash_sale_products" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_flash_sale_products_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE TABLE stock_reservations (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "ReservedAtUtc" timestamp with time zone NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "IsReleased" boolean NOT NULL,
        "ReleasedAtUtc" timestamp with time zone,
        "ReleaseReason" character varying(500),
        CONSTRAINT "PK_stock_reservations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_stock_reservations_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE TABLE customer_purchases (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "FlashSaleProductId" uuid,
        "Quantity" integer NOT NULL,
        "PurchaseDateUtc" timestamp with time zone NOT NULL,
        "OrderId" uuid,
        CONSTRAINT "PK_customer_purchases" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_purchases_flash_sale_products_FlashSaleProductId" FOREIGN KEY ("FlashSaleProductId") REFERENCES flash_sale_products ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_customer_purchases_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId" ON customer_purchases ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId_FlashSaleProductId" ON customer_purchases ("CustomerId", "FlashSaleProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_customer_purchases_CustomerId_ProductId" ON customer_purchases ("CustomerId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_customer_purchases_FlashSaleProductId" ON customer_purchases ("FlashSaleProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_customer_purchases_ProductId" ON customer_purchases ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_flash_sale_products_IsActive_StartTimeUtc_EndTimeUtc" ON flash_sale_products ("IsActive", "StartTimeUtc", "EndTimeUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_flash_sale_products_ProductId" ON flash_sale_products ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_stock_reservations_ExpiresAtUtc_IsReleased" ON stock_reservations ("ExpiresAtUtc", "IsReleased");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_stock_reservations_OrderId" ON stock_reservations ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    CREATE INDEX "IX_stock_reservations_ProductId" ON stock_reservations ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026100953_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251026100953_InitialCreate', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026101134_InitialInventory') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251026101134_InitialInventory', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251026101246_InitialTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251026101246_InitialTables', '9.0.0');
    END IF;
END $EF$;
COMMIT;


# Cart, Checkout & Order Backend Architecture

## Overview

This document details the complete implementation of the Cart, Checkout, and Order backend system for the AI-Powered Multitenant E-commerce Platform. The system is built using **ASP.NET Core + ABP.io** following **DDD (Domain-Driven Design)** principles, with support for **multi-tenancy**, **Stripe payment integration**, and **event-driven architecture**.

### SRS Requirements Covered
- **FR6.x** - Cart functionality
- **FR7.x** - Checkout flow
- **FR3.x** - Order management
- **FR14.5** - Inventory alerts
- **NFR2.x** - Performance (optimistic concurrency)
- **NFR4.x** - Scalability (event-driven)

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            PRESENTATION LAYER                                │
│  ┌─────────────────┐  ┌──────────────────┐  ┌─────────────────┐            │
│  │  CartAppService │  │CheckoutAppService│  │ OrderAppService │            │
│  └────────┬────────┘  └────────┬─────────┘  └────────┬────────┘            │
└───────────┼─────────────────────┼─────────────────────┼────────────────────┘
            │                     │                     │
┌───────────┼─────────────────────┼─────────────────────┼────────────────────┐
│           │            APPLICATION LAYER              │                     │
│  ┌────────▼────────┐  ┌────────▼─────────┐  ┌────────▼────────┐            │
│  │  Cart Operations│  │  Checkout Flow   │  │ Order Queries   │            │
│  └────────┬────────┘  └────────┬─────────┘  └────────┬────────┘            │
│           │           ┌────────▼─────────┐           │                     │
│           │           │ PaymentService   │           │                     │
│           │           │    (Stripe)      │           │                     │
│           │           └────────┬─────────┘           │                     │
└───────────┼─────────────────────┼─────────────────────┼────────────────────┘
            │                     │                     │
┌───────────┼─────────────────────┼─────────────────────┼────────────────────┐
│           │              DOMAIN LAYER                 │                     │
│  ┌────────▼────────┐  ┌────────▼─────────┐  ┌────────▼────────┐            │
│  │  Cart Aggregate │  │ Order Aggregate  │  │  Product Stock  │            │
│  │  ├─ CartItem    │  │ ├─ OrderItem     │  │  (Concurrency)  │            │
│  │  └─ CartStatus  │  │ ├─ Address (VO)  │  │                 │            │
│  │                 │  │ └─ Payment (VO)  │  │                 │            │
│  └─────────────────┘  └──────────────────┘  └─────────────────┘            │
│                              │                                              │
│                    ┌─────────▼─────────┐                                   │
│                    │   Domain Events   │                                   │
│                    │ ├─ OrderPlacedEto │                                   │
│                    │ ├─ StockUpdatedEto│                                   │
│                    │ └─ InventoryLowEto│                                   │
│                    └───────────────────┘                                   │
└─────────────────────────────────────────────────────────────────────────────┘
            │                     │                     │
┌───────────┼─────────────────────┼─────────────────────┼────────────────────┐
│           │           INFRASTRUCTURE                  │                     │
│  ┌────────▼────────┐  ┌────────▼─────────┐  ┌────────▼────────┐            │
│  │   EF Core +     │  │  Stripe Gateway  │  │  ABP Event Bus  │            │
│  │   PostgreSQL    │  │                  │  │  (Distributed)  │            │
│  └─────────────────┘  └──────────────────┘  └─────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## File Structure

```
aspnet-core/src/
├── APME.Domain/
│   ├── Carts/
│   │   ├── Cart.cs                    # Aggregate root
│   │   ├── CartItem.cs                # Cart line item entity
│   │   └── ICartRepository.cs         # Custom repository interface
│   └── Orders/
│       ├── Order.cs                   # Aggregate root
│       ├── OrderItem.cs               # Order line item entity (immutable)
│       ├── Address.cs                 # Value Object
│       ├── PaymentSnapshot.cs         # Value Object
│       └── IOrderRepository.cs        # Custom repository interface
│
├── APME.Domain.Shared/
│   ├── Carts/
│   │   └── CartStatus.cs              # Enum: Active, CheckedOut, Abandoned
│   ├── Orders/
│   │   ├── OrderStatus.cs             # Enum: Pending, Processing, Shipped, etc.
│   │   ├── PaymentStatus.cs           # Enum: Pending, Captured, Failed, etc.
│   │   └── PaymentMethod.cs           # Enum: Card, PayPal, etc.
│   └── Events/
│       ├── OrderPlacedEto.cs          # Order placed event
│       ├── StockUpdatedEto.cs         # Stock changed event
│       └── InventoryLowEto.cs         # Low stock alert event
│
├── APME.Application.Contracts/
│   ├── Carts/
│   │   ├── ICartAppService.cs         # Service interface
│   │   ├── CartViewDto.cs             # Cart display DTO
│   │   ├── AddCartItemInput.cs        # Add item input
│   │   ├── UpdateCartItemInput.cs     # Update quantity input
│   │   └── SetCartNotesInput.cs       # Set notes input
│   ├── Checkout/
│   │   ├── ICheckoutAppService.cs     # Service interface
│   │   ├── CheckoutSummaryDto.cs      # Checkout preview DTO
│   │   ├── AddressDto.cs              # Address input DTO
│   │   ├── CreatePaymentIntentInput.cs # Stripe payment input
│   │   └── PlaceOrderInput.cs         # Place order input
│   └── Orders/
│       ├── IOrderAppService.cs        # Service interface
│       ├── OrderListDto.cs            # Order list item DTO
│       ├── OrderDetailsDto.cs         # Full order details DTO
│       └── GetOrderListInput.cs       # Pagination input
│
├── APME.Application/
│   ├── Carts/
│   │   └── CartAppService.cs          # Cart operations implementation
│   ├── Checkout/
│   │   └── CheckoutAppService.cs      # Checkout flow implementation
│   ├── Orders/
│   │   └── OrderAppService.cs         # Order queries implementation
│   ├── Payments/
│   │   ├── IPaymentService.cs         # Payment abstraction
│   │   ├── StripePaymentService.cs    # Stripe implementation
│   │   └── StripeOptions.cs           # Configuration options
│   └── EventHandlers/
│       ├── OrderPlacedEventHandler.cs # Handles order placed
│       ├── StockUpdatedEventHandler.cs # Handles stock updates
│       └── InventoryLowEventHandler.cs # Handles low stock alerts
│
└── APME.EntityFrameworkCore/
    └── EntityFrameworkCore/
        ├── APMEDbContext.cs           # Updated with Cart/Order configs
        ├── CartRepository.cs          # Custom repository implementation
        └── OrderRepository.cs         # Custom repository implementation
```

---

## Domain Models

### Cart Aggregate Root

**File:** `APME.Domain/Carts/Cart.cs`

```csharp
public class Cart : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CustomerId { get; private set; }
    public Guid ShopId { get; private set; }
    public CartStatus Status { get; private set; }  // Active, CheckedOut, Abandoned
    public ICollection<CartItem> Items { get; private set; }
    public string? Notes { get; private set; }
}
```

**Key Domain Methods:**
- `AddItem(productId, name, sku, price, quantity, imageUrl)` - Adds or updates item
- `UpdateItemQuantity(cartItemId, newQuantity)` - Updates quantity, removes if 0
- `RemoveItem(cartItemId)` - Removes specific item
- `Clear()` - Removes all items
- `MarkAsCheckedOut()` - Changes status after order placement
- `GetSubTotal()` - Calculates cart total
- `RefreshItemPrices(priceMap)` - Updates prices from current product prices

**Business Rules:**
- One active cart per customer per shop (enforced via unique index)
- Cart is mutable while status is Active
- Cannot modify cart after CheckedOut

### CartItem Entity

**File:** `APME.Domain/Carts/CartItem.cs`

```csharp
public class CartItem : Entity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string ProductName { get; private set; }      // Snapshot
    public string ProductSku { get; private set; }       // Snapshot
    public string? ProductImageUrl { get; private set; } // Snapshot
}
```

### Order Aggregate Root

**File:** `APME.Domain/Orders/Order.cs`

```csharp
public class Order : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CustomerId { get; private set; }
    public Guid ShopId { get; private set; }
    public string OrderNumber { get; private set; }     // e.g., ORD-2024-000001
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; } // Value Object
    public Address? BillingAddress { get; private set; }
    public PaymentSnapshot Payment { get; private set; } // Value Object
    public ICollection<OrderItem> Items { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; }
    public string? CustomerNotes { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? ShippingCarrier { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
}
```

**Status Transitions:**
```
Pending → PaymentConfirmed → Processing → Shipped → Delivered
    ↓           ↓               ↓           ↓
PaymentFailed  Cancelled      Cancelled   Cancelled → Refunded
```

### Address Value Object

**File:** `APME.Domain/Orders/Address.cs`

```csharp
public class Address : ValueObject
{
    public string FullName { get; private set; }
    public string Street { get; private set; }
    public string? Street2 { get; private set; }
    public string City { get; private set; }
    public string? State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }  // ISO 3166-1 alpha-2
    public string? Phone { get; private set; }
}
```

### PaymentSnapshot Value Object

**File:** `APME.Domain/Orders/PaymentSnapshot.cs`

```csharp
public class PaymentSnapshot : ValueObject
{
    public PaymentMethod Method { get; private set; }
    public string TransactionId { get; private set; }  // Stripe PaymentIntent ID
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public string? CardLast4 { get; private set; }
    public string? CardBrand { get; private set; }
    public string? FailureMessage { get; private set; }
}
```

---

## Product Entity Enhancement

**File:** `APME.Domain/Products/Product.cs`

Added fields for stock management with optimistic concurrency:

```csharp
// New fields added
public int LowStockThreshold { get; set; } = 10;

[ConcurrencyCheck]
public string StockConcurrencyStamp { get; protected set; } = Guid.NewGuid().ToString("N");
```

**New Methods:**

```csharp
// Atomic stock deduction with BusinessException on insufficient stock
public void DeductStockAtomic(int quantity)
{
    if (quantity <= 0)
        throw new ArgumentException("Quantity must be positive");
    if (StockQuantity < quantity)
        throw new BusinessException("APME:InsufficientStock")
            .WithData("ProductId", Id)
            .WithData("Available", StockQuantity)
            .WithData("Requested", quantity);
    
    StockQuantity -= quantity;
    UpdateStockConcurrencyStamp();
}

// Restore stock when order is cancelled
public void RestoreStock(int quantity);

// Check if stock is below threshold
public bool IsLowStock();

// Set low stock threshold
public void SetLowStockThreshold(int threshold);
```

---

## Domain Events

### OrderPlacedEto

**File:** `APME.Domain.Shared/Events/OrderPlacedEto.cs`

Published when an order is successfully placed.

```csharp
[EventName("APME.Order.Placed")]
public class OrderPlacedEto
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ShopId { get; set; }
    public Guid? TenantId { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; }
    public DateTime PlacedAt { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public List<OrderItemEto> Items { get; set; }
}
```

**Used For:**
- Sending order confirmation emails
- Updating analytics
- Cache invalidation

### StockUpdatedEto

**File:** `APME.Domain.Shared/Events/StockUpdatedEto.cs`

Published when product stock changes.

```csharp
[EventName("APME.Stock.Updated")]
public class StockUpdatedEto
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public Guid? TenantId { get; set; }
    public string ProductName { get; set; }
    public string ProductSku { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public StockUpdateReason Reason { get; set; }  // OrderPlaced, OrderCancelled, etc.
    public Guid? ReferenceId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Used For:**
- Checking if stock fell below threshold
- Publishing InventoryLowEto if needed
- Cache invalidation

### InventoryLowEto

**File:** `APME.Domain.Shared/Events/InventoryLowEto.cs`

Published when stock falls below threshold (FR14.5).

```csharp
[EventName("APME.Inventory.Low")]
public class InventoryLowEto
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public Guid? TenantId { get; set; }
    public string ProductName { get; set; }
    public string ProductSku { get; set; }
    public int CurrentQuantity { get; set; }
    public int ThresholdQuantity { get; set; }
    public InventoryAlertSeverity Severity { get; set; }  // Low, Critical, OutOfStock
    public DateTime AlertedAt { get; set; }
    public string? ProductImageUrl { get; set; }
}
```

**Used For:**
- Notifying shop admins
- Triggering reorder alerts
- Hiding out-of-stock products

---

## Application Services

### ICartAppService

**File:** `APME.Application.Contracts/Carts/ICartAppService.cs`

| Method | SRS Ref | Description |
|--------|---------|-------------|
| `GetCurrentCartAsync(shopId)` | FR6.4 | Get customer's active cart |
| `AddItemAsync(input)` | FR6.1 | Add product to cart |
| `UpdateItemQuantityAsync(input)` | FR6.2 | Update item quantity |
| `UpdateItemQuantityByProductAsync(input)` | FR6.2 | Update by product ID |
| `RemoveItemAsync(cartItemId)` | FR6.3 | Remove item |
| `RemoveItemByProductAsync(shopId, productId)` | FR6.3 | Remove by product ID |
| `ClearCartAsync(shopId)` | - | Clear all items |
| `SetNotesAsync(input)` | - | Set customer notes |
| `GetItemCountAsync(shopId)` | - | Get item count for badge |
| `RefreshPricesAsync(shopId)` | - | Update prices from products |
| `ValidateForCheckoutAsync(shopId)` | - | Validate stock/availability |

### ICheckoutAppService

**File:** `APME.Application.Contracts/Checkout/ICheckoutAppService.cs`

| Method | SRS Ref | Description |
|--------|---------|-------------|
| `GetCheckoutSummaryAsync(shopId)` | FR7.4 | Preview order with totals |
| `CreatePaymentIntentAsync(input)` | FR7.1 | Create Stripe PaymentIntent |
| `PlaceOrderAsync(input)` | FR7.1, UC11 | Complete checkout atomically |
| `CancelPaymentIntentAsync(paymentIntentId)` | - | Cancel pending payment |
| `HandleStripeWebhookAsync(payload, signature)` | - | Handle Stripe webhooks |

### IOrderAppService

**File:** `APME.Application.Contracts/Orders/IOrderAppService.cs`

| Method | SRS Ref | Description |
|--------|---------|-------------|
| `GetListAsync(input)` | FR3.1 | List customer orders (paginated) |
| `GetAsync(id)` | FR3.3 | Get order details by ID |
| `GetByOrderNumberAsync(orderNumber)` | FR3.3 | Get by order number |
| `CancelAsync(id, reason)` | - | Cancel order (restores stock) |
| `GetCountAsync(shopId)` | - | Get order count |

---

## Checkout Flow (PlaceOrderAsync)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PlaceOrderAsync Flow                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  1. Validate Cart Not Empty   │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  2. Verify Payment Status     │
                    │     (Stripe PaymentIntent)    │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  3. Load Products & Validate  │
                    │     Stock Availability        │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  4. Calculate Totals          │
                    │     (SubTotal + Tax + Ship)   │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  5. Create Address & Payment  │
                    │     Value Objects             │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  6. Generate Order Number     │
                    │     (ORD-YYYY-NNNNNN)         │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  7. Create Order Aggregate    │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │     FOR EACH Cart Item:        │
                    │  ┌─────────────────────────┐  │
                    │  │ 8. DeductStockAtomic()  │  │
                    │  │    (with concurrency)   │  │
                    │  └───────────┬─────────────┘  │
                    │              │                 │
                    │  ┌───────────▼─────────────┐  │
                    │  │ 9. Add OrderItem        │  │
                    │  └───────────┬─────────────┘  │
                    │              │                 │
                    │  ┌───────────▼─────────────┐  │
                    │  │ 10. Publish             │  │
                    │  │     StockUpdatedEto     │  │
                    │  └─────────────────────────┘  │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  11. Confirm Payment Status   │
                    │      (order.ConfirmPayment()) │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  12. Save Order to DB         │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  13. Mark Cart as CheckedOut  │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  14. Publish OrderPlacedEto   │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  15. Return PlaceOrderResult  │
                    │      with OrderId & Number    │
                    └───────────────────────────────┘
```

---

## Database Schema

### New Tables

| Table | Description |
|-------|-------------|
| `AppCarts` | Cart aggregate storage |
| `AppCartItems` | Cart line items |
| `AppOrders` | Order aggregate storage |
| `AppOrderItems` | Order line items (immutable snapshot) |

### EF Core Configuration

**File:** `APME.EntityFrameworkCore/EntityFrameworkCore/APMEDbContext.cs`

#### Cart Configuration

```csharp
builder.Entity<Cart>(b =>
{
    b.ToTable(APMEConsts.DbTablePrefix + "Carts", APMEConsts.DbSchema);
    b.ConfigureByConvention();
    
    // Unique constraint: one active cart per customer per shop
    b.HasIndex(x => new { x.CustomerId, x.ShopId, x.Status })
        .HasFilter("\"Status\" = 0")  // Active status
        .IsUnique();
    
    b.HasMany(x => x.Items)
        .WithOne()
        .HasForeignKey(x => x.CartId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

#### Order Configuration

```csharp
builder.Entity<Order>(b =>
{
    b.ToTable(APMEConsts.DbTablePrefix + "Orders", APMEConsts.DbSchema);
    b.ConfigureByConvention();
    
    // Owned entities for value objects
    b.OwnsOne(x => x.ShippingAddress, a => { /* column mappings */ });
    b.OwnsOne(x => x.BillingAddress, a => { /* column mappings */ });
    b.OwnsOne(x => x.Payment, p => { /* column mappings */ });
    
    // Indexes for performance
    b.HasIndex(x => x.OrderNumber).IsUnique();
    b.HasIndex(x => new { x.ShopId, x.Status });
    b.HasIndex(x => new { x.CustomerId, x.CreationTime });
});
```

#### Product Enhancement

```csharp
// Added to Product configuration
b.Property(x => x.StockConcurrencyStamp)
    .IsRequired()
    .HasMaxLength(40)
    .IsConcurrencyToken();
b.Property(x => x.LowStockThreshold).HasDefaultValue(10);
```

---

## Stripe Integration

### Configuration

**File:** `appsettings.json`

```json
{
  "Stripe": {
    "SecretKey": "sk_test_YOUR_SECRET_KEY_HERE",
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY_HERE",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET_HERE",
    "TestMode": true,
    "Currency": "usd"
  }
}
```

### StripePaymentService

**File:** `APME.Application/Payments/StripePaymentService.cs`

```csharp
public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, 
        string currency, 
        Guid orderId,
        string? customerEmail = null,
        Dictionary<string, string>? metadata = null);
    
    Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId);
    Task CancelPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentStatusResult> GetPaymentIntentStatusAsync(string paymentIntentId);
    Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string signature);
}
```

### Payment Flow

```
Frontend                    Backend                         Stripe
   │                           │                               │
   │ 1. GetCheckoutSummary     │                               │
   │─────────────────────────►│                               │
   │◄─────────────────────────│                               │
   │    CheckoutSummaryDto     │                               │
   │                           │                               │
   │ 2. CreatePaymentIntent    │                               │
   │─────────────────────────►│                               │
   │                           │ 3. POST /payment_intents     │
   │                           │─────────────────────────────►│
   │                           │◄─────────────────────────────│
   │◄─────────────────────────│    PaymentIntent created      │
   │    { clientSecret }       │                               │
   │                           │                               │
   │ 4. stripe.confirmPayment  │                               │
   │──────────────────────────────────────────────────────────►│
   │◄──────────────────────────────────────────────────────────│
   │    Payment confirmed      │                               │
   │                           │                               │
   │ 5. PlaceOrder             │                               │
   │─────────────────────────►│                               │
   │                           │ 6. GET PaymentIntent status  │
   │                           │─────────────────────────────►│
   │                           │◄─────────────────────────────│
   │                           │    status: succeeded          │
   │                           │                               │
   │                           │ 7. Create Order               │
   │                           │ 8. Deduct Stock               │
   │                           │ 9. Publish Events             │
   │◄─────────────────────────│                               │
   │    PlaceOrderResult       │                               │
```

---

## Event Handlers

### OrderPlacedEventHandler

**File:** `APME.Application/EventHandlers/OrderPlacedEventHandler.cs`

**Responsibilities:**
- Send order confirmation email to customer
- Update analytics/metrics
- Invalidate relevant caches (product listings)

### StockUpdatedEventHandler

**File:** `APME.Application/EventHandlers/StockUpdatedEventHandler.cs`

**Responsibilities:**
- Check if new quantity is below threshold
- Determine severity (Low, Critical, OutOfStock)
- Publish `InventoryLowEto` if threshold crossed
- Invalidate product cache

### InventoryLowEventHandler

**File:** `APME.Application/EventHandlers/InventoryLowEventHandler.cs`

**Responsibilities:**
- Notify shop admin via in-app notification
- Send email alert to shop admin
- For OutOfStock: consider hiding product from storefront

---

## API Endpoints (ABP Auto-Generated)

### Cart APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/app/cart/current/{shopId}` | Get current cart |
| POST | `/api/app/cart/add-item` | Add item to cart |
| PUT | `/api/app/cart/update-quantity` | Update item quantity |
| PUT | `/api/app/cart/update-quantity-by-product` | Update by product ID |
| DELETE | `/api/app/cart/remove-item/{id}` | Remove item |
| DELETE | `/api/app/cart/remove-item-by-product/{shopId}/{productId}` | Remove by product |
| DELETE | `/api/app/cart/clear/{shopId}` | Clear cart |
| PUT | `/api/app/cart/notes` | Set customer notes |
| GET | `/api/app/cart/item-count/{shopId}` | Get item count |
| POST | `/api/app/cart/refresh-prices/{shopId}` | Refresh prices |
| POST | `/api/app/cart/validate/{shopId}` | Validate for checkout |

### Checkout APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/app/checkout/summary/{shopId}` | Get checkout summary |
| POST | `/api/app/checkout/payment-intent` | Create PaymentIntent |
| POST | `/api/app/checkout/place-order` | Place order |
| DELETE | `/api/app/checkout/payment-intent/{id}` | Cancel PaymentIntent |
| POST | `/api/app/checkout/stripe-webhook` | Handle Stripe webhook |

### Order APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/app/order` | List orders (paginated) |
| GET | `/api/app/order/{id}` | Get order by ID |
| GET | `/api/app/order/by-number/{orderNumber}` | Get by order number |
| POST | `/api/app/order/{id}/cancel` | Cancel order |
| GET | `/api/app/order/count` | Get order count |

---

## DTOs

### CartViewDto

```csharp
public class CartViewDto : EntityDto<Guid>
{
    public Guid ShopId { get; set; }
    public List<CartItemViewDto> Items { get; set; }
    public int TotalItems { get; set; }
    public int UniqueItemCount { get; set; }
    public decimal SubTotal { get; set; }
    public bool HasOutOfStockItems { get; set; }
    public bool IsEmpty { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class CartItemViewDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductSku { get; set; }
    public string? ProductImageUrl { get; set; }
    public string? ProductSlug { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CurrentPrice { get; set; }  // May differ if price changed
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
    public bool ExceedsStock { get; set; }
    public bool PriceChanged { get; set; }
    public bool IsProductAvailable { get; set; }
}
```

### CheckoutSummaryDto

```csharp
public class CheckoutSummaryDto
{
    public CartViewDto Cart { get; set; }
    public decimal SubTotal { get; set; }
    public decimal EstimatedTax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; }
    public bool CanCheckout { get; set; }
    public List<string> ValidationErrors { get; set; }
    public List<ShippingOptionDto> ShippingOptions { get; set; }
}
```

### OrderDetailsDto

```csharp
public class OrderDetailsDto : EntityDto<Guid>
{
    public string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public DateTime CreationTime { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public AddressDto? BillingAddress { get; set; }
    public PaymentInfoDto Payment { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; }
    public string? CustomerNotes { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingCarrier { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool CanBeCancelled { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
}
```

---

## Error Handling

### Business Exceptions

| Exception Code | Description |
|----------------|-------------|
| `APME:NotAuthenticated` | User is not logged in |
| `APME:CustomerNotFound` | Customer record not found |
| `APME:CartNotFound` | Cart not found |
| `APME:CartItemNotFound` | Cart item not found |
| `APME:CartNotActive` | Cart is not in Active status |
| `APME:ProductNotAvailable` | Product is not active/published |
| `APME:InsufficientStock` | Not enough stock available |
| `APME:OrderNotFound` | Order not found |
| `APME:CannotCancelOrder` | Order cannot be cancelled |
| `APME:InvalidOrderStatusTransition` | Invalid status change |
| `APME:CannotModifyOrder` | Order cannot be modified |
| `APME:CannotReactivateCheckedOutCart` | Checked out cart cannot be reactivated |

### PlaceOrderErrorCode Enum

```csharp
public enum PlaceOrderErrorCode
{
    Unknown,
    EmptyCart,
    PaymentFailed,
    PaymentNotConfirmed,
    InsufficientStock,
    ProductNotAvailable,
    ConcurrencyConflict,
    InvalidAddress
}
```

---

## Configuration

### Module Registration

**File:** `APME.Application/APMEApplicationModule.cs`

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var configuration = context.Services.GetConfiguration();
    
    // Configure Stripe options
    context.Services.Configure<StripeOptions>(
        configuration.GetSection(StripeOptions.SectionName));
    
    // Register payment service
    context.Services.AddTransient<IPaymentService, StripePaymentService>();
}
```

### Repository Registration

**File:** `APME.EntityFrameworkCore/EntityFrameworkCore/APMEEntityFrameworkCoreModule.cs`

```csharp
context.Services.AddAbpDbContext<APMEDbContext>(options =>
{
    options.AddDefaultRepositories(includeAllEntities: true);
    
    // Register custom repositories
    options.AddRepository<Cart, CartRepository>();
    options.AddRepository<Order, OrderRepository>();
});
```

### NuGet Package

**File:** `APME.Application/APME.Application.csproj`

```xml
<PackageReference Include="Stripe.net" Version="45.14.0" />
```

---

## Next Steps

1. **Database Migration:**
   ```bash
   cd aspnet-core/src/APME.DbMigrator
   dotnet ef migrations add AddCartAndOrder -p ../APME.EntityFrameworkCore
   dotnet run
   ```

2. **Configure Stripe:**
   - Create Stripe account at https://stripe.com
   - Get test API keys from Dashboard
   - Update `appsettings.json` with real keys
   - Set up webhook endpoint for payment confirmations

3. **Implement Email Service:**
   - Create `IEmailService` interface
   - Implement email sending in `OrderPlacedEventHandler`
   - Implement admin notifications in `InventoryLowEventHandler`

4. **Frontend Integration:**
   - Create cart page component
   - Create checkout flow components
   - Integrate Stripe.js for payment
   - Create order history and details pages

5. **Testing:**
   - Unit tests for domain methods
   - Integration tests for checkout flow
   - Test concurrent stock deduction

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-12-29 | Initial implementation of Cart, Checkout, Order backend |


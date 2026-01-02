using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Carts;
using APME.Customers;
using APME.Events;
using APME.Orders;
using APME.Payments;
using APME.Products;
using APME.Shops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace APME.Checkout;

/// <summary>
/// Application service for checkout operations
/// Supports multi-shop cart - one cart can contain items from multiple shops
/// </summary>
[Authorize]
public class CheckoutAppService : ApplicationService, ICheckoutAppService
{
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly IPaymentService _paymentService;
    private readonly IDistributedEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<CheckoutAppService> _logger;
    private readonly IImageUrlProvider _imageUrlProvider;

    public CheckoutAppService(
        ICartRepository cartRepository,
        IOrderRepository orderRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Shop, Guid> shopRepository,
        IPaymentService paymentService,
        IDistributedEventBus eventBus,
        ICurrentUser currentUser,
        IDataFilter dataFilter,
        ILogger<CheckoutAppService> logger,
        IImageUrlProvider imageUrlProvider)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _shopRepository = shopRepository;
        _paymentService = paymentService;
        _eventBus = eventBus;
        _currentUser = currentUser;
        _dataFilter = dataFilter;
        _logger = logger;
        _imageUrlProvider = imageUrlProvider;
    }

    /// <inheritdoc />
    public async Task<CheckoutSummaryDto> GetCheckoutSummaryAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null || cart.IsEmpty())
        {
            return new CheckoutSummaryDto
            {
                Cart = new CartViewDto { IsEmpty = true, Items = new List<CartItemViewDto>(), ShopGroups = new List<CartShopGroupDto>() },
                CanCheckout = false,
                ValidationErrors = new List<string> { "Your cart is empty" }
            };
        }

        // Validate cart
        var validation = await ValidateCartForCheckoutAsync(cart);

        // Calculate totals
        var subTotal = cart.GetSubTotal();
        var estimatedTax = subTotal * 0.08m; // 8% tax (placeholder)
        var shippingCost = 5.99m; // Flat rate shipping (placeholder)
        var total = subTotal + estimatedTax + shippingCost;

        return new CheckoutSummaryDto
        {
            Cart = await MapToCartViewDtoAsync(cart),
            SubTotal = subTotal,
            EstimatedTax = Math.Round(estimatedTax, 2),
            ShippingCost = shippingCost,
            DiscountAmount = 0,
            Total = Math.Round(total, 2),
            Currency = "USD",
            CanCheckout = validation.IsValid,
            ValidationErrors = validation.Errors,
            ShippingOptions = new List<ShippingOptionDto>
            {
                new ShippingOptionDto
                {
                    Id = "standard",
                    Name = "Standard Shipping",
                    Description = "5-7 business days",
                    Price = 5.99m,
                    EstimatedDelivery = "5-7 business days"
                },
                new ShippingOptionDto
                {
                    Id = "express",
                    Name = "Express Shipping",
                    Description = "2-3 business days",
                    Price = 12.99m,
                    EstimatedDelivery = "2-3 business days"
                }
            }
        };
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var customer = await _customerRepository.GetAsync(customerId);
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null || cart.IsEmpty())
        {
            return new PaymentIntentResult
            {
                Success = false,
                ErrorMessage = "Your cart is empty"
            };
        }

        // Validate cart
        var validation = await ValidateCartForCheckoutAsync(cart);
        if (!validation.IsValid)
        {
            return new PaymentIntentResult
            {
                Success = false,
                ErrorMessage = string.Join("; ", validation.Errors)
            };
        }

        // Calculate total
        var subTotal = cart.GetSubTotal();
        var tax = subTotal * 0.08m;
        var shipping = 5.99m;
        var total = subTotal + tax + shipping;

        // Get the shop IDs from cart items for metadata
        var shopIds = string.Join(",", cart.Items.Select(i => i.ShopId).Distinct());

        // Create payment intent
        var result = await _paymentService.CreatePaymentIntentAsync(
            total,
            "usd",
            cart.Id,
            customer.Email,
            new Dictionary<string, string>
            {
                { "cart_id", cart.Id.ToString() },
                { "customer_id", customerId.ToString() },
                { "shop_ids", shopIds }
            });

        return result;
    }

    /// <inheritdoc />
    [UnitOfWork]
    public async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderInput input)
    {
        try
        {
            var customerId = await GetCurrentCustomerIdAsync();
            var customer = await _customerRepository.GetAsync(customerId);
            var cart = await _cartRepository.GetActiveCartAsync(customerId);

            // Validate cart exists and is not empty
            if (cart == null || cart.IsEmpty())
            {
                return new PlaceOrderResult
                {
                    Success = false,
                    ErrorCode = PlaceOrderErrorCode.EmptyCart,
                    ErrorMessage = "Your cart is empty"
                };
            }

            // Verify payment status
            var paymentStatus = await _paymentService.GetPaymentIntentStatusAsync(input.PaymentIntentId);
            
            if (!paymentStatus.IsSucceeded)
            {
                return new PlaceOrderResult
                {
                    Success = false,
                    ErrorCode = PlaceOrderErrorCode.PaymentNotConfirmed,
                    ErrorMessage = paymentStatus.ErrorMessage ?? "Payment has not been confirmed"
                };
            }

            // Get products and validate stock (with concurrency check)
            var productIds = cart.Items.Select(x => x.ProductId).ToList();
            Dictionary<Guid, Product> productMap;

            using (_dataFilter.Disable<IMultiTenant>())
            {
                var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
                productMap = products.ToDictionary(p => p.Id, p => p);
            }

            // Validate stock availability
            foreach (var item in cart.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    return new PlaceOrderResult
                    {
                        Success = false,
                        ErrorCode = PlaceOrderErrorCode.ProductNotAvailable,
                        ErrorMessage = $"Product '{item.ProductName}' is no longer available"
                    };
                }

                if (!product.IsActive || !product.IsPublished)
                {
                    return new PlaceOrderResult
                    {
                        Success = false,
                        ErrorCode = PlaceOrderErrorCode.ProductNotAvailable,
                        ErrorMessage = $"Product '{item.ProductName}' is no longer available"
                    };
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return new PlaceOrderResult
                    {
                        Success = false,
                        ErrorCode = PlaceOrderErrorCode.InsufficientStock,
                        ErrorMessage = $"Insufficient stock for '{item.ProductName}'. Available: {product.StockQuantity}"
                    };
                }
            }

            // Calculate totals
            var subTotal = cart.GetSubTotal();
            var taxAmount = Math.Round(subTotal * 0.08m, 2);
            var shippingAmount = 5.99m;
            var totalAmount = subTotal + taxAmount + shippingAmount;

            // Create addresses
            var shippingAddress = new Address(
                input.ShippingAddress.FullName,
                input.ShippingAddress.Street,
                input.ShippingAddress.City,
                input.ShippingAddress.PostalCode,
                input.ShippingAddress.Country,
                input.ShippingAddress.Street2,
                input.ShippingAddress.State,
                input.ShippingAddress.Phone);

            Address? billingAddress = null;
            if (input.BillingAddress != null)
            {
                billingAddress = new Address(
                    input.BillingAddress.FullName,
                    input.BillingAddress.Street,
                    input.BillingAddress.City,
                    input.BillingAddress.PostalCode,
                    input.BillingAddress.Country,
                    input.BillingAddress.Street2,
                    input.BillingAddress.State,
                    input.BillingAddress.Phone);
            }

            // Create payment snapshot
            var paymentSnapshot = PaymentSnapshot.CreateSuccessful(
                input.PaymentIntentId,
                totalAmount,
                "USD",
                paymentStatus.CardLast4,
                paymentStatus.CardBrand);

            // Generate order number (now global, not per-shop)
            var orderNumber = await _orderRepository.GetNextOrderNumberAsync();

            // Create order (host-level, no shopId - supports multi-shop)
            var order = new Order(
                GuidGenerator.Create(),
                customerId,
                orderNumber,
                shippingAddress,
                paymentSnapshot,
                "USD",
                billingAddress,
                input.CustomerNotes ?? cart.Notes);

            // Add order items and deduct stock atomically
            foreach (var cartItem in cart.Items)
            {
                var product = productMap[cartItem.ProductId];
                
                // Deduct stock with optimistic concurrency
                product.DeductStockAtomic(cartItem.Quantity);
                
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    await _productRepository.UpdateAsync(product);
                }

                // Calculate item tax
                var itemTax = Math.Round(cartItem.GetLineTotal() * 0.08m, 2);

                // Get the image URL, converting blob name to full URL for order snapshot
                var orderItemImageUrl = _imageUrlProvider.GetFullImageUrl(
                    cartItem.ProductImageUrl ?? product.PrimaryImageUrl);

                // Add item with shopId (multi-shop support)
                order.AddItem(
                    cartItem.ShopId, // ShopId is now per-item
                    product.Id,
                    cartItem.ProductName,
                    cartItem.ProductSku,
                    cartItem.Quantity,
                    cartItem.UnitPrice,
                    0, // No item-level discount
                    itemTax,
                    orderItemImageUrl);

                // Publish stock updated event
                await _eventBus.PublishAsync(new StockUpdatedEto
                {
                    ProductId = product.Id,
                    ShopId = cartItem.ShopId,
                    TenantId = CurrentTenant.Id,
                    ProductName = product.Name,
                    ProductSku = product.SKU,
                    OldQuantity = product.StockQuantity + cartItem.Quantity,
                    NewQuantity = product.StockQuantity,
                    Reason = StockUpdateReason.OrderPlaced,
                    ReferenceId = order.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Set shipping amount
            order.SetShippingAmount(shippingAmount);

            // Confirm payment
            order.ConfirmPayment();

            // Save order
            await _orderRepository.InsertAsync(order);

            // Mark cart as checked out
            cart.MarkAsCheckedOut();
            await _cartRepository.UpdateAsync(cart);

            // Publish order placed event
            await _eventBus.PublishAsync(new OrderPlacedEto
            {
                OrderId = order.Id,
                CustomerId = customerId,
                ShopIds = order.GetShopIds().ToList(),
                OrderNumber = orderNumber,
                TotalAmount = totalAmount,
                Currency = "USD",
                PlacedAt = DateTime.UtcNow,
                CustomerEmail = customer.Email,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                Items = order.Items.Select(i => new OrderItemEto
                {
                    ShopId = i.ShopId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductSku = i.ProductSku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            });

            _logger.LogInformation(
                "Order {OrderNumber} placed successfully for customer {CustomerId}",
                orderNumber, customerId);

            return new PlaceOrderResult
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = orderNumber
            };
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order");
            return new PlaceOrderResult
            {
                Success = false,
                ErrorCode = PlaceOrderErrorCode.Unknown,
                ErrorMessage = "An error occurred while placing your order. Please try again."
            };
        }
    }

    /// <inheritdoc />
    public async Task CancelPaymentIntentAsync(string paymentIntentId)
    {
        await _paymentService.CancelPaymentIntentAsync(paymentIntentId);
    }

    /// <inheritdoc />
    [AllowAnonymous] // Webhooks don't have auth
    public async Task HandleStripeWebhookAsync(string payload, string signature)
    {
        var result = await _paymentService.ProcessWebhookAsync(payload, signature);
        
        if (!result.Success)
        {
            _logger.LogWarning("Failed to process Stripe webhook: {Error}", result.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("Processed Stripe webhook: {EventType}", result.EventType);
        }
    }

    #region Helper Methods

    private async Task<Guid> GetCurrentCustomerIdAsync()
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new BusinessException("APME:NotAuthenticated");
        }

        // Customer ID is the same as the ABP User ID
        var customer = await _customerRepository.FindAsync(_currentUser.Id.Value);

        if (customer == null)
        {
            throw new BusinessException("APME:CustomerNotFound");
        }

        return customer.Id;
    }

    private async Task<(bool IsValid, List<string> Errors)> ValidateCartForCheckoutAsync(Cart cart)
    {
        var errors = new List<string>();

        var productIds = cart.Items.Select(x => x.ProductId).ToList();
        List<Product> products;

        using (_dataFilter.Disable<IMultiTenant>())
        {
            products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
        }

        var productMap = products.ToDictionary(p => p.Id, p => p);

        foreach (var item in cart.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                errors.Add($"Product '{item.ProductName}' is no longer available");
                continue;
            }

            if (!product.IsActive || !product.IsPublished)
            {
                errors.Add($"Product '{item.ProductName}' is no longer available");
                continue;
            }

            if (product.StockQuantity < item.Quantity)
            {
                errors.Add(product.StockQuantity == 0
                    ? $"Product '{item.ProductName}' is out of stock"
                    : $"Only {product.StockQuantity} units of '{item.ProductName}' available");
            }
        }

        return (errors.Count == 0, errors);
    }

    private async Task<CartViewDto> MapToCartViewDtoAsync(Cart cart)
    {
        var productIds = cart.Items.Select(x => x.ProductId).ToList();
        var shopIds = cart.Items.Select(x => x.ShopId).Distinct().ToList();

        List<Product> products;
        List<Shop> shops;

        using (_dataFilter.Disable<IMultiTenant>())
        {
            products = productIds.Any()
                ? await _productRepository.GetListAsync(p => productIds.Contains(p.Id))
                : new List<Product>();

            shops = shopIds.Any()
                ? await _shopRepository.GetListAsync(s => shopIds.Contains(s.Id))
                : new List<Shop>();
        }

        var productMap = products.ToDictionary(p => p.Id, p => p);
        var shopMap = shops.ToDictionary(s => s.Id, s => s);

        var itemDtos = cart.Items.Select(item =>
        {
            productMap.TryGetValue(item.ProductId, out var product);
            shopMap.TryGetValue(item.ShopId, out var shop);

            // Get the image URL, converting blob name to full URL
            var imageUrl = item.ProductImageUrl ?? product?.PrimaryImageUrl;
            var fullImageUrl = _imageUrlProvider.GetFullImageUrl(imageUrl);

            return new CartItemViewDto
            {
                Id = item.Id,
                ShopId = item.ShopId,
                ShopName = shop?.Name ?? "Unknown Shop",
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                ProductImageUrl = fullImageUrl,
                ProductSlug = product?.Slug,
                UnitPrice = item.UnitPrice,
                CurrentPrice = product?.Price ?? item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.GetLineTotal(),
                AvailableStock = product?.StockQuantity ?? 0,
                IsInStock = product != null && product.StockQuantity > 0,
                ExceedsStock = product != null && product.StockQuantity < item.Quantity,
                PriceChanged = product != null && product.Price != item.UnitPrice,
                IsProductAvailable = product != null && product.IsActive && product.IsPublished
            };
        }).ToList();

        // Group items by shop
        var shopGroups = itemDtos
            .GroupBy(i => i.ShopId)
            .Select(g => new CartShopGroupDto
            {
                ShopId = g.Key,
                ShopName = g.First().ShopName,
                Items = g.ToList(),
                SubTotal = g.Sum(i => i.LineTotal),
                ItemCount = g.Sum(i => i.Quantity)
            })
            .ToList();

        return new CartViewDto
        {
            Id = cart.Id,
            Items = itemDtos,
            ShopGroups = shopGroups,
            TotalItems = cart.GetTotalItemCount(),
            UniqueItemCount = cart.Items.Count,
            ShopCount = shopIds.Count,
            SubTotal = cart.GetSubTotal(),
            HasOutOfStockItems = itemDtos.Any(i => !i.IsInStock || i.ExceedsStock),
            IsEmpty = cart.IsEmpty(),
            Notes = cart.Notes,
            LastModificationTime = cart.LastModificationTime
        };
    }

    #endregion
}

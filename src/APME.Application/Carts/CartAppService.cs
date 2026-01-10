using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Customers;
using APME.Products;
using APME.Shops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace APME.Carts;

/// <summary>
/// Application service for cart operations
/// Cart is at host level - one per customer, can contain items from multiple shops
/// </summary>
[Authorize(AuthenticationSchemes = APMEConsts.AuthenticationSchema)]
public class CartAppService : ApplicationService, ICartAppService
{
    private readonly ICartRepository _cartRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IDataFilter _dataFilter;
    private readonly IImageUrlProvider _imageUrlProvider;

    public CartAppService(
        ICartRepository cartRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Shop, Guid> shopRepository,
        ICurrentUser currentUser,
        IDataFilter dataFilter,
        IImageUrlProvider imageUrlProvider)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _shopRepository = shopRepository;
        _currentUser = currentUser;
        _dataFilter = dataFilter;
        _imageUrlProvider = imageUrlProvider;
    }

    /// <inheritdoc />
    public async Task<CartViewDto> GetCurrentCartAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            // Create new cart
            cart = new Cart(GuidGenerator.Create(), customerId);
            await _cartRepository.InsertAsync(cart);
        }

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> AddItemAsync(AddCartItemInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();

        // Get or create cart
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            cart = new Cart(GuidGenerator.Create(), customerId);
        }

        // Get product (disable tenant filter to access products from any tenant/shop)
        Product product;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            product = await _productRepository.GetAsync(input.ProductId);
        }

        if (!product.IsActive || !product.IsPublished)
        {
            throw new BusinessException("APME:ProductNotAvailable")
                .WithData("ProductId", input.ProductId);
        }

        // Check stock
        if (product.StockQuantity < input.Quantity)
        {
            throw new BusinessException("APME:InsufficientStock")
                .WithData("ProductId", input.ProductId)
                .WithData("Available", product.StockQuantity)
                .WithData("Requested", input.Quantity);
        }

        // Add item to cart with shopId
        cart.AddItem(
            product.ShopId,
            product.Id,
            product.Name,
            product.SKU,
            product.Price,
            input.Quantity,
            product.PrimaryImageUrl);

        await _cartRepository.UpdateAsync(cart , autoSave:true);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> UpdateItemQuantityAsync(UpdateCartItemInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();

        // Find the cart that contains this item
        var cart = await _cartRepository.GetByCartItemIdAsync(customerId, input.CartItemId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartItemNotFound")
                .WithData("CartItemId", input.CartItemId);
        }

        var cartItem = cart.Items.First(x => x.Id == input.CartItemId);

        if (input.Quantity > 0)
        {
            // Check stock
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var product = await _productRepository.FindAsync(cartItem.ProductId);
                if (product != null && product.StockQuantity < input.Quantity)
                {
                    throw new BusinessException("APME:InsufficientStock")
                        .WithData("ProductId", cartItem.ProductId)
                        .WithData("Available", product.StockQuantity)
                        .WithData("Requested", input.Quantity);
                }
            }
        }

        cart.UpdateItemQuantity(input.CartItemId, input.Quantity);
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> UpdateItemQuantityByProductAsync(UpdateCartItemByProductInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartNotFound");
        }

        if (input.Quantity > 0)
        {
            // Check stock
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var product = await _productRepository.FindAsync(input.ProductId);
                if (product != null && product.StockQuantity < input.Quantity)
                {
                    throw new BusinessException("APME:InsufficientStock")
                        .WithData("ProductId", input.ProductId)
                        .WithData("Available", product.StockQuantity)
                        .WithData("Requested", input.Quantity);
                }
            }
        }

        cart.UpdateItemQuantityByProduct(input.ShopId, input.ProductId, input.Quantity);
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> RemoveItemAsync(Guid cartItemId)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetByCartItemIdAsync(customerId, cartItemId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartItemNotFound")
                .WithData("CartItemId", cartItemId);
        }

        cart.RemoveItem(cartItemId);
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> RemoveItemByProductAsync(Guid shopId, Guid productId)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartNotFound");
        }

        cart.RemoveItemByProduct(shopId, productId);
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    [HttpDelete("clear")]
    public async Task<CartViewDto> ClearCartAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartNotFound");
        }

        cart.Clear();
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartViewDto> SetNotesAsync(SetCartNotesInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null)
        {
            throw new BusinessException("APME:CartNotFound");
        }

        cart.SetNotes(input.Notes);
        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<int> GetItemCountAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        return cart?.GetTotalItemCount() ?? 0;
    }

    /// <inheritdoc />
    public async Task<CartViewDto> RefreshPricesAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        if (cart == null || cart.IsEmpty())
        {
            return await MapToCartViewDtoAsync(cart);
        }

        // Get current prices for all products in cart
        var productIds = cart.Items.Select(x => x.ProductId).ToList();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            var priceMap = products.ToDictionary(p => p.Id, p => p.Price);
            cart.RefreshItemPrices(priceMap);
        }

        await _cartRepository.UpdateAsync(cart);

        return await MapToCartViewDtoAsync(cart);
    }

    /// <inheritdoc />
    public async Task<CartValidationResult> ValidateForCheckoutAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var cart = await _cartRepository.GetActiveCartAsync(customerId);

        var result = new CartValidationResult
        {
            IsValid = true,
            Errors = new List<CartValidationError>()
        };

        if (cart == null || cart.IsEmpty())
        {
            result.IsValid = false;
            result.Errors.Add(new CartValidationError
            {
                Type = CartValidationErrorType.EmptyCart,
                Message = "Your cart is empty"
            });
            return result;
        }

        // Get all products (disable tenant filter)
        var productIds = cart.Items.Select(x => x.ProductId).ToList();
        Dictionary<Guid, Product> productMap;

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            productMap = products.ToDictionary(p => p.Id, p => p);
        }

        foreach (var item in cart.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                result.IsValid = false;
                result.Errors.Add(new CartValidationError
                {
                    Type = CartValidationErrorType.ProductNotFound,
                    ShopId = item.ShopId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Message = $"Product '{item.ProductName}' is no longer available"
                });
                continue;
            }

            if (!product.IsActive || !product.IsPublished)
            {
                result.IsValid = false;
                result.Errors.Add(new CartValidationError
                {
                    Type = CartValidationErrorType.ProductNotAvailable,
                    ShopId = item.ShopId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Message = $"Product '{item.ProductName}' is no longer available"
                });
                continue;
            }

            if (product.StockQuantity < item.Quantity)
            {
                result.IsValid = false;
                result.Errors.Add(new CartValidationError
                {
                    Type = CartValidationErrorType.InsufficientStock,
                    ShopId = item.ShopId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Message = product.StockQuantity == 0
                        ? $"Product '{item.ProductName}' is out of stock"
                        : $"Only {product.StockQuantity} units of '{item.ProductName}' available"
                });
            }

            if (product.Price != item.UnitPrice)
            {
                // Price changed - not blocking but notify
                result.Errors.Add(new CartValidationError
                {
                    Type = CartValidationErrorType.PriceChanged,
                    ShopId = item.ShopId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Message = $"Price for '{item.ProductName}' has changed from {item.UnitPrice:C} to {product.Price:C}"
                });
            }
        }

        result.Cart = await MapToCartViewDtoAsync(cart);
        return result;
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

    private async Task<CartViewDto> MapToCartViewDtoAsync(Cart? cart)
    {
        if (cart == null)
        {
            return new CartViewDto
            {
                Items = new List<CartItemViewDto>(),
                ShopGroups = new List<CartShopGroupDto>(),
                TotalItems = 0,
                UniqueItemCount = 0,
                ShopCount = 0,
                SubTotal = 0,
                HasOutOfStockItems = false,
                IsEmpty = true
            };
        }

        // Get current product data (disable tenant filter)
        var productIds = cart.Items.Select(x => x.ProductId).ToList();
        var shopIds = cart.Items.Select(x => x.ShopId).Distinct().ToList();

        Dictionary<Guid, Product> productMap = new();
        Dictionary<Guid, Shop> shopMap = new();

        using (_dataFilter.Disable<IMultiTenant>())
        {
            if (productIds.Any())
            {
                var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
                productMap = products.ToDictionary(p => p.Id, p => p);
            }

            if (shopIds.Any())
            {
                var shops = await _shopRepository.GetListAsync(s => shopIds.Contains(s.Id));
                shopMap = shops.ToDictionary(s => s.Id, s => s);
            }
        }

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

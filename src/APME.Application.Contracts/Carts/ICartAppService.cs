using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.Carts;

/// <summary>
/// Application service interface for cart operations
/// All operations require authenticated customer
/// Cart is at host level - one per customer, can contain items from multiple shops
/// </summary>
public interface ICartAppService : IApplicationService
{
    /// <summary>
    /// Gets the current customer's active cart
    /// Creates a new cart if one doesn't exist
    /// FR6.4 - View cart contents
    /// </summary>
    Task<CartViewDto> GetCurrentCartAsync();

    /// <summary>
    /// Adds a product to the cart (with shop info)
    /// If the product already exists, increases the quantity
    /// FR6.1 - Add items to cart
    /// </summary>
    Task<CartViewDto> AddItemAsync(AddCartItemInput input);

    /// <summary>
    /// Updates the quantity of an existing cart item
    /// Setting quantity to 0 removes the item
    /// FR6.2 - Update item quantity
    /// </summary>
    Task<CartViewDto> UpdateItemQuantityAsync(UpdateCartItemInput input);

    /// <summary>
    /// Updates the quantity of an item by product ID and shop ID
    /// Setting quantity to 0 removes the item
    /// FR6.2 - Update item quantity
    /// </summary>
    Task<CartViewDto> UpdateItemQuantityByProductAsync(UpdateCartItemByProductInput input);

    /// <summary>
    /// Removes an item from the cart by cart item ID
    /// FR6.3 - Remove items from cart
    /// </summary>
    Task<CartViewDto> RemoveItemAsync(Guid cartItemId);

    /// <summary>
    /// Removes an item from the cart by shop and product ID
    /// FR6.3 - Remove items from cart
    /// </summary>
    Task<CartViewDto> RemoveItemByProductAsync(Guid shopId, Guid productId);

    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    Task<CartViewDto> ClearCartAsync();

    /// <summary>
    /// Sets customer notes for the order
    /// </summary>
    Task<CartViewDto> SetNotesAsync(SetCartNotesInput input);

    /// <summary>
    /// Gets the item count for the current cart (for header badge)
    /// </summary>
    Task<int> GetItemCountAsync();

    /// <summary>
    /// Refreshes cart prices from current product prices
    /// </summary>
    Task<CartViewDto> RefreshPricesAsync();

    /// <summary>
    /// Validates cart for checkout (checks stock, prices, product availability)
    /// </summary>
    Task<CartValidationResult> ValidateForCheckoutAsync();
}

/// <summary>
/// Result of cart validation for checkout
/// </summary>
public class CartValidationResult
{
    /// <summary>
    /// Whether the cart is valid for checkout
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<CartValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Updated cart with current stock/price information
    /// </summary>
    public CartViewDto? Cart { get; set; }
}

/// <summary>
/// A validation error for cart checkout
/// </summary>
public class CartValidationError
{
    /// <summary>
    /// The type of error
    /// </summary>
    public CartValidationErrorType Type { get; set; }

    /// <summary>
    /// The shop ID if applicable
    /// </summary>
    public Guid? ShopId { get; set; }

    /// <summary>
    /// The product ID if applicable
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// The product name if applicable
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Types of cart validation errors
/// </summary>
public enum CartValidationErrorType
{
    EmptyCart,
    ProductNotFound,
    ProductNotAvailable,
    InsufficientStock,
    PriceChanged
}

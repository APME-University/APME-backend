using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Carts;

/// <summary>
/// Input for updating a cart item's quantity
/// </summary>
public class UpdateCartItemInput
{
    /// <summary>
    /// The cart item ID
    /// </summary>
    [Required]
    public Guid CartItemId { get; set; }

    /// <summary>
    /// New quantity (0 to remove the item)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Quantity must be between 0 and 100")]
    public int Quantity { get; set; }
}

/// <summary>
/// Input for updating a cart item's quantity by product ID
/// </summary>
public class UpdateCartItemByProductInput
{
    /// <summary>
    /// The shop ID
    /// </summary>
    [Required]
    public Guid ShopId { get; set; }

    /// <summary>
    /// The product ID
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// New quantity (0 to remove the item)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Quantity must be between 0 and 100")]
    public int Quantity { get; set; }
}


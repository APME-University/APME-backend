using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Carts;

/// <summary>
/// Input for adding an item to the cart
/// </summary>
public class AddCartItemInput
{
    /// <summary>
    /// The shop ID (required for multi-shop support)
    /// </summary>
    [Required]
    public Guid ShopId { get; set; }

    /// <summary>
    /// The product to add
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity to add (default: 1)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;
}


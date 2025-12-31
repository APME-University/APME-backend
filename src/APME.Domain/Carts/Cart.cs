using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace APME.Carts;

/// <summary>
/// Cart Aggregate Root
/// Represents a customer's shopping cart at the host level (not tenant-specific)
/// Supports items from multiple shops in a single cart
/// Business Rule: One active cart per customer (enforced via unique index)
/// </summary>
public class Cart : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The customer who owns this cart
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Current status of the cart
    /// </summary>
    public CartStatus Status { get; private set; }

    /// <summary>
    /// Items in the cart (can be from multiple shops)
    /// </summary>
    public ICollection<CartItem> Items { get; private set; }

    /// <summary>
    /// Optional notes from the customer
    /// </summary>
    public string? Notes { get; private set; }

    protected Cart()
    {
        Items = new List<CartItem>();
    }

    public Cart(
        Guid id,
        Guid customerId) : base(id)
    {
        CustomerId = customerId;
        Status = CartStatus.Active;
        Items = new List<CartItem>();
    }

    /// <summary>
    /// Adds a new item to the cart or updates quantity if already exists
    /// Items can be from different shops
    /// </summary>
    public CartItem AddItem(
        Guid shopId,
        Guid productId,
        string productName,
        string productSku,
        decimal unitPrice,
        int quantity,
        string? productImageUrl = null)
    {
        EnsureCartIsActive();

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        // Check if item already exists (same product from same shop)
        var existingItem = Items.FirstOrDefault(x => x.ProductId == productId && x.ShopId == shopId);
        
        if (existingItem != null)
        {
            // Update existing item quantity
            existingItem.SetQuantity(existingItem.Quantity + quantity);
            existingItem.UpdatePrice(unitPrice);
            existingItem.UpdateProductInfo(productName, productImageUrl);
            return existingItem;
        }

        // Add new item
        var newItem = new CartItem(
            Guid.NewGuid(),
            Id,
            shopId,
            productId,
            productName,
            productSku,
            unitPrice,
            quantity,
            productImageUrl);

        Items.Add(newItem);
        return newItem;
    }

    /// <summary>
    /// Updates the quantity of an existing cart item
    /// </summary>
    public void UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        EnsureCartIsActive();

        var item = Items.FirstOrDefault(x => x.Id == cartItemId);
        
        if (item == null)
        {
            throw new BusinessException("APME:CartItemNotFound")
                .WithData("CartItemId", cartItemId);
        }

        if (newQuantity <= 0)
        {
            // Remove item if quantity is zero or negative
            Items.Remove(item);
        }
        else
        {
            item.SetQuantity(newQuantity);
        }
    }

    /// <summary>
    /// Updates the quantity of an item by product ID and shop ID
    /// </summary>
    public void UpdateItemQuantityByProduct(Guid shopId, Guid productId, int newQuantity)
    {
        EnsureCartIsActive();

        var item = Items.FirstOrDefault(x => x.ProductId == productId && x.ShopId == shopId);
        
        if (item == null)
        {
            throw new BusinessException("APME:CartItemNotFound")
                .WithData("ProductId", productId)
                .WithData("ShopId", shopId);
        }

        if (newQuantity <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.SetQuantity(newQuantity);
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    public void RemoveItem(Guid cartItemId)
    {
        EnsureCartIsActive();

        var item = Items.FirstOrDefault(x => x.Id == cartItemId);
        
        if (item == null)
        {
            throw new BusinessException("APME:CartItemNotFound")
                .WithData("CartItemId", cartItemId);
        }

        Items.Remove(item);
    }

    /// <summary>
    /// Removes an item from the cart by product ID and shop ID
    /// </summary>
    public void RemoveItemByProduct(Guid shopId, Guid productId)
    {
        EnsureCartIsActive();

        var item = Items.FirstOrDefault(x => x.ProductId == productId && x.ShopId == shopId);
        
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    public void Clear()
    {
        EnsureCartIsActive();
        Items.Clear();
    }

    /// <summary>
    /// Marks the cart as checked out (converted to order)
    /// </summary>
    public void MarkAsCheckedOut()
    {
        EnsureCartIsActive();
        Status = CartStatus.CheckedOut;
    }

    /// <summary>
    /// Marks the cart as abandoned
    /// </summary>
    public void MarkAsAbandoned()
    {
        if (Status != CartStatus.Active)
        {
            return; // Ignore if already checked out
        }
        
        Status = CartStatus.Abandoned;
    }

    /// <summary>
    /// Reactivates an abandoned cart
    /// </summary>
    public void Reactivate()
    {
        if (Status == CartStatus.CheckedOut)
        {
            throw new BusinessException("APME:CannotReactivateCheckedOutCart");
        }
        
        Status = CartStatus.Active;
    }

    /// <summary>
    /// Sets customer notes for the order
    /// </summary>
    public void SetNotes(string? notes)
    {
        Notes = notes?.Trim();
    }

    /// <summary>
    /// Gets the total number of items in the cart
    /// </summary>
    public int GetTotalItemCount()
    {
        return Items.Sum(x => x.Quantity);
    }

    /// <summary>
    /// Gets the subtotal of the cart
    /// </summary>
    public decimal GetSubTotal()
    {
        return Items.Sum(x => x.GetLineTotal());
    }

    /// <summary>
    /// Checks if the cart is empty
    /// </summary>
    public bool IsEmpty()
    {
        return !Items.Any();
    }

    /// <summary>
    /// Checks if the cart contains a specific product from a specific shop
    /// </summary>
    public bool ContainsProduct(Guid shopId, Guid productId)
    {
        return Items.Any(x => x.ProductId == productId && x.ShopId == shopId);
    }

    /// <summary>
    /// Gets a cart item by product ID and shop ID
    /// </summary>
    public CartItem? GetItemByProduct(Guid shopId, Guid productId)
    {
        return Items.FirstOrDefault(x => x.ProductId == productId && x.ShopId == shopId);
    }

    /// <summary>
    /// Gets all unique shop IDs in the cart
    /// </summary>
    public IEnumerable<Guid> GetShopIds()
    {
        return Items.Select(x => x.ShopId).Distinct();
    }

    /// <summary>
    /// Gets items grouped by shop
    /// </summary>
    public IEnumerable<IGrouping<Guid, CartItem>> GetItemsByShop()
    {
        return Items.GroupBy(x => x.ShopId);
    }

    /// <summary>
    /// Refreshes item prices from current product prices
    /// </summary>
    public void RefreshItemPrices(Dictionary<Guid, decimal> productPrices)
    {
        foreach (var item in Items)
        {
            if (productPrices.TryGetValue(item.ProductId, out var currentPrice))
            {
                item.UpdatePrice(currentPrice);
            }
        }
    }

    private void EnsureCartIsActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new BusinessException("APME:CartNotActive")
                .WithData("CartId", Id)
                .WithData("Status", Status);
        }
    }
}

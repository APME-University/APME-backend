using System;

namespace APME.Dashboard;

public class DashboardStatisticsDto
{
    /// <summary>
    /// Total number of categories for the current tenant
    /// </summary>
    public int TotalCategories { get; set; }

    /// <summary>
    /// Total number of products for the current tenant
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// Number of active products for the current tenant
    /// </summary>
    public int ActiveProducts { get; set; }

    /// <summary>
    /// Number of published products for the current tenant
    /// </summary>
    public int PublishedProducts { get; set; }

    /// <summary>
    /// Number of active categories for the current tenant
    /// </summary>
    public int ActiveCategories { get; set; }

    /// <summary>
    /// Total number of orders for the current tenant
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Total number of customers for the current tenant
    /// </summary>
    public int TotalCustomers { get; set; }

    /// <summary>
    /// Low stock products count (products with stock quantity less than threshold)
    /// </summary>
    public int LowStockProducts { get; set; }

    /// <summary>
    /// Out of stock products count
    /// </summary>
    public int OutOfStockProducts { get; set; }

    /// <summary>
    /// Date when the statistics were last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

public class HostDashboardStatisticsDto
{
    /// <summary>
    /// Total number of shops in the system
    /// </summary>
    public int TotalShops { get; set; }

    /// <summary>
    /// Number of active shops
    /// </summary>
    public int ActiveShops { get; set; }

    /// <summary>
    /// Number of inactive shops
    /// </summary>
    public int InactiveShops { get; set; }

    /// <summary>
    /// Total number of tenants
    /// </summary>
    public int TotalTenants { get; set; }

    /// <summary>
    /// Total number of promo codes
    /// </summary>
    public int TotalPromoCodes { get; set; }

    /// <summary>
    /// Number of active promo codes
    /// </summary>
    public int ActivePromoCodes { get; set; }

    /// <summary>
    /// Total number of addresses
    /// </summary>
    public int TotalAddresses { get; set; }

    /// <summary>
    /// Total number of payments
    /// </summary>
    public int TotalPayments { get; set; }

    /// <summary>
    /// Date when the statistics were last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

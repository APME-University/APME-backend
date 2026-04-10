using System;
using System.Linq;
using System.Threading.Tasks;
using APME.Categories;
using APME.Dashboard;
using APME.Products;
using APME.Shops;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Dashboard;

public class DashboardAppService : ApplicationService, IDashboardAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly ILogger<DashboardAppService> _logger;

    public DashboardAppService(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Shop, Guid> shopRepository,
        ILogger<DashboardAppService> logger)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _shopRepository = shopRepository;
        _logger = logger;
    }

    public async Task<DashboardStatisticsDto> GetTenantDashboardStatisticsAsync()
    {
        var currentTenantId = CurrentTenant.Id;
        
        if (currentTenantId == null)
        {
            _logger.LogWarning("GetTenantDashboardStatisticsAsync called without a current tenant");
            return new DashboardStatisticsDto
            {
                LastUpdated = DateTime.UtcNow
            };
        }

        try
        {
            // Get categories statistics for current tenant
            var categoriesQueryable = await _categoryRepository.GetQueryableAsync();
            var totalCategories = await AsyncExecuter.CountAsync(
                categoriesQueryable.Where(c => c.TenantId == currentTenantId));
            var activeCategories = await AsyncExecuter.CountAsync(
                categoriesQueryable.Where(c => c.TenantId == currentTenantId && c.IsActive));

            // Get products statistics for current tenant
            var productsQueryable = await _productRepository.GetQueryableAsync();
            var totalProducts = await AsyncExecuter.CountAsync(
                productsQueryable.Where(p => p.TenantId == currentTenantId));
            var activeProducts = await AsyncExecuter.CountAsync(
                productsQueryable.Where(p => p.TenantId == currentTenantId && p.IsActive));
            var publishedProducts = await AsyncExecuter.CountAsync(
                productsQueryable.Where(p => p.TenantId == currentTenantId && p.IsPublished));
            
            // Low stock products (less than 10 items)
            var lowStockThreshold = 10;
            var lowStockProducts = await AsyncExecuter.CountAsync(
                productsQueryable.Where(p => 
                    p.TenantId == currentTenantId && 
                    p.IsActive && 
                    p.StockQuantity > 0 && 
                    p.StockQuantity <= lowStockThreshold));
            
            // Out of stock products
            var outOfStockProducts = await AsyncExecuter.CountAsync(
                productsQueryable.Where(p => 
                    p.TenantId == currentTenantId && 
                    p.IsActive && 
                    p.StockQuantity <= 0));

            _logger.LogInformation(
                "Retrieved tenant dashboard statistics for tenant {TenantId}: " +
                "Categories: {TotalCategories}/{ActiveCategories}, " +
                "Products: {TotalProducts}/{ActiveProducts}/{PublishedProducts}, " +
                "Low Stock: {LowStockProducts}, Out of Stock: {OutOfStockProducts}",
                currentTenantId, totalCategories, activeCategories,
                totalProducts, activeProducts, publishedProducts,
                lowStockProducts, outOfStockProducts);

            return new DashboardStatisticsDto
            {
                TotalCategories = totalCategories,
                ActiveCategories = activeCategories,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                PublishedProducts = publishedProducts,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                // TODO: Implement when Orders and Customers modules are ready
                TotalOrders = 0,
                TotalCustomers = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant dashboard statistics for tenant {TenantId}", currentTenantId);
            throw;
        }
    }

    public async Task<HostDashboardStatisticsDto> GetHostDashboardStatisticsAsync()
    {
        try
        {
            // Get shops statistics (host level)
            var shopsQueryable = await _shopRepository.GetQueryableAsync();
            var totalShops = await AsyncExecuter.CountAsync(shopsQueryable);
            var activeShops = await AsyncExecuter.CountAsync(shopsQueryable.Where(s => s.IsActive));
            var inactiveShops = totalShops - activeShops;

            // Get categories statistics (host level)
            var categoriesQueryable = await _categoryRepository.GetQueryableAsync();
            var totalCategories = await AsyncExecuter.CountAsync(categoriesQueryable);
            var activeCategories = await AsyncExecuter.CountAsync(categoriesQueryable.Where(c => c.IsActive));

            // Get products statistics (host level)
            var productsQueryable = await _productRepository.GetQueryableAsync();
            var totalProducts = await AsyncExecuter.CountAsync(productsQueryable);
            var activeProducts = await AsyncExecuter.CountAsync(productsQueryable.Where(p => p.IsActive));
            var publishedProducts = await AsyncExecuter.CountAsync(productsQueryable.Where(p => p.IsPublished));

            _logger.LogInformation(
                "Retrieved host dashboard statistics: " +
                "Shops: {TotalShops}/{ActiveShops}/{InactiveShops}, " +
                "Categories: {TotalCategories}/{ActiveCategories}, " +
                "Products: {TotalProducts}/{ActiveProducts}/{PublishedProducts}",
                totalShops, activeShops, inactiveShops,
                totalCategories, activeCategories,
                totalProducts, activeProducts, publishedProducts);

            return new HostDashboardStatisticsDto
            {
                TotalShops = totalShops,
                ActiveShops = activeShops,
                InactiveShops = inactiveShops,
                // TODO: Implement when other modules are ready
                TotalTenants = 0,
                TotalPromoCodes = 0,
                ActivePromoCodes = 0,
                TotalAddresses = 0,
                TotalPayments = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving host dashboard statistics");
            throw;
        }
    }
}

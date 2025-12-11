using System;
using System.Linq;
using System.Threading.Tasks;
using APME.Shops;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using System.Collections.Generic;
using Volo.Abp.Data;

namespace APME.Shops;

public class ShopAppService : CrudAppService<Shop, ShopDto, Guid, GetShopListInput, CreateUpdateShopDto>, IShopAppService
{
    private readonly ITenantAppService _tenantAppService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDataFilter _dataFilter;
    public ShopAppService(
        IRepository<Shop, Guid> repository,
        ITenantAppService tenantAppService,
        IUnitOfWorkManager unitOfWorkManager,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        IDataFilter dataFilter) : base(repository)
    {
        _tenantAppService = tenantAppService;
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    public override async Task<ShopDto> CreateAsync(CreateUpdateShopDto input)
    {
        // Validate tenant creation properties are provided
        if (string.IsNullOrWhiteSpace(input.TenantName) ||
            string.IsNullOrWhiteSpace(input.AdminEmail) ||
            string.IsNullOrWhiteSpace(input.AdminPassword))
        {
            throw new ArgumentException("Tenant name, admin email, and admin password are required for shop creation.");
        }

        // Check if tenant name already exists
        var existingTenant = await _tenantRepository.FindByNameAsync(input.TenantName);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"A tenant with the name '{input.TenantName}' already exists.");
        }

        TenantDto createdTenant;

        try
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            
            // Create tenant first
            var tenantCreateDto = new TenantCreateDto
            {
                Name = input.TenantName,
                AdminEmailAddress = input.AdminEmail,
                AdminPassword = input.AdminPassword
            };

            createdTenant = await _tenantAppService.CreateAsync(tenantCreateDto);

            // Create shop with tenant ID
            var shop = new Shop(
                GuidGenerator.Create(),
                createdTenant.Id,
                input.Name,
                input.Slug
            );

            // Set optional properties
            shop.Description = input.Description;
            shop.IsActive = input.IsActive;
            shop.LogoUrl = input.LogoUrl;
            shop.Settings = input.Settings;

            await Repository.InsertAsync(shop);

            await uow.CompleteAsync();

            // Map to DTO and include tenant information
            var shopDto = ObjectMapper.Map<Shop, ShopDto>(shop);
            shopDto.TenantId = createdTenant.Id;
            shopDto.TenantName = createdTenant.Name;

            return shopDto;
        }
        catch (Exception ex)
        {
            // If shop creation fails, tenant will be rolled back by transaction
            Logger.LogError(ex, "Error creating shop with tenant. Tenant: {TenantName}", input.TenantName);
            throw;
        }
    }

    public override async Task<ShopDto> UpdateAsync(Guid id, CreateUpdateShopDto input)
    {
        var shop = await Repository.GetAsync(id);
        
        // Ensure tenant ID cannot be changed
        if (shop.TenantId.HasValue && input.TenantName != null)
        {
            throw new InvalidOperationException("Tenant information cannot be changed after shop creation.");
        }

        // Update shop properties only
        shop.UpdateName(input.Name);
        shop.UpdateSlug(input.Slug);
        shop.Description = input.Description;
        shop.IsActive = input.IsActive;
        shop.LogoUrl = input.LogoUrl;
        shop.Settings = input.Settings;

        await Repository.UpdateAsync(shop);

        // Map to DTO and include tenant information if available
        var shopDto = ObjectMapper.Map<Shop, ShopDto>(shop);
        
        if (shop.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.FindAsync(shop.TenantId.Value);
            if (tenant != null)
            {
                shopDto.TenantId = tenant.Id;
                shopDto.TenantName = tenant.Name;
            }
        }

        return shopDto;
    }

    public override async Task<ShopDto> GetAsync(Guid id)
    {
        var shopDto = await base.GetAsync(id);
        
        // Include tenant information
        if (shopDto.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.FindAsync(shopDto.TenantId.Value);
            if (tenant != null)
            {
                shopDto.TenantName = tenant.Name;
            }
        }

        return shopDto;
    }

    public override async Task<PagedResultDto<ShopDto>> GetListAsync(GetShopListInput input)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await CheckGetListPolicyAsync().ConfigureAwait(continueOnCapturedContext: false);
            IQueryable<Shop> query = await CreateFilteredQueryAsync(input).ConfigureAwait(continueOnCapturedContext: false);
            int totalCount = await base.AsyncExecuter.CountAsync(query).ConfigureAwait(continueOnCapturedContext: false);
            new List<Shop>();
            List<ShopDto> items = new List<ShopDto>();
            if (totalCount > 0)
            {
                query = ApplySorting(query, input);
                query = ApplyPaging(query, input);
                items = await MapToGetListOutputDtosAsync(await base.AsyncExecuter.ToListAsync(query).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
            }

            var result = new PagedResultDto<ShopDto>(totalCount, items);
            // Include tenant names for all shops
            if (result.Items != null && result.Items.Any())
            {
                var tenantIds = result.Items.Where(x => x.TenantId.HasValue).Select(x => x.TenantId.Value).Distinct().ToList();
                var tenants = await _tenantRepository.GetListAsync();
                var tenantDict = tenants.Where(t => tenantIds.Contains(t.Id)).ToDictionary(t => t.Id, t => t.Name);

                foreach (var shopDto in result.Items)
                {
                    if (shopDto.TenantId.HasValue && tenantDict.TryGetValue(shopDto.TenantId.Value, out var tenantName))
                    {
                        shopDto.TenantName = tenantName;
                    }
                }
            }

            return result;
        }
    }

    public async Task<ShopDto> ActivateAsync(Guid id)
    {
        var shop = await Repository.GetAsync(id);
        shop.Activate();
        await Repository.UpdateAsync(shop);
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    public async Task<ShopDto> DeactivateAsync(Guid id)
    {
        var shop = await Repository.GetAsync(id);
        shop.Deactivate();
        await Repository.UpdateAsync(shop);
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }
}


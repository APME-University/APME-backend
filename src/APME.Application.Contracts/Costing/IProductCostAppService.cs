using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.Costing;

public interface IProductCostAppService : IApplicationService
{
    Task<ProductCostDto> GetByProductAsync(Guid productId);

    Task<ProductCostDto> SetCostAsync(SetProductCostInput input);

    Task RecordReceiptAsync(RecordReceiptInput input);

    Task<UnitEconomicsDto> GetUnitEconomicsAsync(Guid productId, Guid shopId, decimal price);

    Task<CostImportResultDto> ImportCostsAsync(List<CostImportRow> rows);
}

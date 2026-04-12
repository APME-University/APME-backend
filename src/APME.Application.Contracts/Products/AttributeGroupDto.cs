using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class AttributeGroupDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }

    public Guid? CategoryId { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation display
    public string? CategoryName { get; set; }

    public int AttributeCount { get; set; }
}

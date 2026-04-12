using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductTagDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }

    public string Tag { get; set; }

    // Navigation display
    public string? ProductName { get; set; }
}

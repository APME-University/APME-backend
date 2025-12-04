using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateProductAttributeDto
{
    [Required]
    public Guid ShopId { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string DisplayName { get; set; }

    [Required]
    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }
}


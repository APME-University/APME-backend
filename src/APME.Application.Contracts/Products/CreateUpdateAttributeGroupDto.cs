using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateAttributeGroupDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public Guid? CategoryId { get; set; }

    public int DisplayOrder { get; set; }
}

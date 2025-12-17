using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Categories;

public class UploadCategoryImageDto
{
    [Required]
    public Guid CategoryId { get; set; }
}





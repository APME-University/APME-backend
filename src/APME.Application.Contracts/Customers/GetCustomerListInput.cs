using System;
using Volo.Abp.Application.Dtos;

namespace APME.Customers;

public class GetCustomerListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? UserId { get; set; }

    public bool? IsActive { get; set; }
}


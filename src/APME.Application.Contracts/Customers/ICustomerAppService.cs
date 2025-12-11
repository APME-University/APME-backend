using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Customers;

public interface ICustomerAppService : ICrudAppService<CustomerDto, Guid, GetCustomerListInput, CreateUpdateCustomerDto>
{
}


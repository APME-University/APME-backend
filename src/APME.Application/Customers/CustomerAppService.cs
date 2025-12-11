using System;
using APME.Customers;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Customers;

public class CustomerAppService : CrudAppService<Customer, CustomerDto, Guid, GetCustomerListInput, CreateUpdateCustomerDto>, ICustomerAppService
{
    public CustomerAppService(IRepository<Customer, Guid> repository) : base(repository)
    {
    }
}


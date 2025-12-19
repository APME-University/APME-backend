using Volo.Abp.Application.Dtos;

namespace APME.Customers;

public class CustomerLoginResponseDto
{
    public string Token { get; set; }
    public CustomerDto Customer { get; set; }
}

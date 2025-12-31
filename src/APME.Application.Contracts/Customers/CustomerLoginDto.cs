using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace APME.Customers;

public class CustomerLoginDto
{
    [Required]
    public string EmailOrPhone { get; set; }

    [Required]
    public string Password { get; set; }

    public Guid? TenantId { get; set; }

    public static string ConvertArabicToEnglishNumbers(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace('٠', '0')
                    .Replace('١', '1')
                    .Replace('٢', '2')
                    .Replace('٣', '3')
                    .Replace('٤', '4')
                    .Replace('٥', '5')
                    .Replace('٦', '6')
                    .Replace('٧', '7')
                    .Replace('٨', '8')
                    .Replace('٩', '9');
    }
}

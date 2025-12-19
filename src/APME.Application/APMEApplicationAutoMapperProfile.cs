using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using APME.Shops;
using APME.Customers;
using APME.Categories;
using APME.Products;

namespace APME;

public class APMEApplicationAutoMapperProfile : Profile
{
    public APMEApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        // Shop mappings
        CreateMap<Shop, ShopDto>();
        CreateMap<CreateUpdateShopDto, Shop>();

        // Customer mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateUpdateCustomerDto, Customer>();

        // Category mappings
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateUpdateCategoryDto, Category>();

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.ImageUrls, opt => opt.Ignore()) // Ignore automatic mapping, handle manually in AfterMap
            .AfterMap((src, dest) =>
            {
                // Manually map ImageUrls from JSON string to List<string>
                if (string.IsNullOrWhiteSpace(src.ImageUrls))
                {
                    dest.ImageUrls = new List<string>();
                }
                else
                {
                    try
                    {
                        dest.ImageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(src.ImageUrls) ?? new List<string>();
                    }
                    catch
                    {
                        dest.ImageUrls = new List<string>();
                    }
                }
            });
        
        CreateMap<CreateUpdateProductDto, Product>()
            .ForMember(dest => dest.ImageUrls, opt => opt.Ignore()) // ImageUrls handled separately via upload endpoints
            .ForMember(dest => dest.PrimaryImageUrl, opt => opt.Ignore()) // PrimaryImageUrl handled separately
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore()); // Preserve ConcurrencyStamp for updates

        // ProductAttribute mappings
        CreateMap<ProductAttribute, ProductAttributeDto>();
        CreateMap<CreateUpdateProductAttributeDto, ProductAttribute>();
    }
}

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
        CreateMap<Product, ProductDto>();
        CreateMap<CreateUpdateProductDto, Product>();

        // ProductAttribute mappings
        CreateMap<ProductAttribute, ProductAttributeDto>();
        CreateMap<CreateUpdateProductAttributeDto, ProductAttribute>();
    }
}

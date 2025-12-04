using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using APME;
using APME.Shops;
using APME.Customers;
using APME.Categories;
using APME.Products;

namespace APME.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class APMEDbContext :
    AbpDbContext<APMEDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    // E-commerce entities
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public APMEDbContext(DbContextOptions<APMEDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        ConfigureShops(builder);
        ConfigureCustomers(builder);
        ConfigureCategories(builder);
        ConfigureProducts(builder);
        ConfigureProductAttributes(builder);
    }

    private void ConfigureShops(ModelBuilder builder)
    {
        builder.Entity<Shop>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Shops", APMEConsts.DbSchema);

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(256);
            b.Property(x => x.LogoUrl).HasMaxLength(512);
            b.Property(x => x.Settings).HasColumnType("jsonb");

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.Slug);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        });
    }

    private void ConfigureCustomers(ModelBuilder builder)
    {
        builder.Entity<Customer>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Customers", APMEConsts.DbSchema);

            b.Property(x => x.FirstName).IsRequired().HasMaxLength(128);
            b.Property(x => x.LastName).IsRequired().HasMaxLength(128);
            b.Property(x => x.PhoneNumber).HasMaxLength(32);

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        });
    }

    private void ConfigureCategories(ModelBuilder builder)
    {
        builder.Entity<Category>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Categories", APMEConsts.DbSchema);

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.ImageUrl).HasMaxLength(512);

            // Foreign keys
            b.HasOne<Shop>()
                .WithMany()
                .HasForeignKey(x => x.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.Slug);
            b.HasIndex(x => x.ParentId);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
        });
    }

    private void ConfigureProducts(ModelBuilder builder)
    {
        builder.Entity<Product>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Products", APMEConsts.DbSchema);

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.SKU).IsRequired().HasMaxLength(128);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.CompareAtPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.Attributes).HasColumnType("jsonb");

            // Foreign keys
            b.HasOne<Shop>()
                .WithMany()
                .HasForeignKey(x => x.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Category>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.Slug);
            b.HasIndex(x => x.SKU);
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.SKU }).IsUnique();
        });
    }

    private void ConfigureProductAttributes(ModelBuilder builder)
    {
        builder.Entity<ProductAttribute>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "ProductAttributes", APMEConsts.DbSchema);

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(256);
            b.Property(x => x.DataType).IsRequired();

            // Foreign keys
            b.HasOne<Shop>()
                .WithMany()
                .HasForeignKey(x => x.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.Name);
            b.HasIndex(x => new { x.TenantId, x.ShopId, x.Name }).IsUnique();
        });
    }
}

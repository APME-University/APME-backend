using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using APME;
using APME.AI;
using APME.Shops;
using APME.Customers;
using APME.Categories;
using APME.Products;
using APME.Carts;
using APME.Orders;
using APME.Chat;

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
    public DbSet<CustomerUserRole> CustomerUserRoles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }

    // Cart entities
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    // Order entities
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // AI/RAG entities
    public DbSet<ProductEmbedding> ProductEmbeddings { get; set; }

    // Chat entities
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

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

        /* Enable pgvector extension for AI embeddings */
        builder.HasPostgresExtension("vector");

        /* Ignore Value Objects as standalone entities - they are owned types */
        builder.Ignore<PaymentSnapshot>();
        builder.Ignore<Address>();

        /* Configure your own tables/entities inside here */

        ConfigureShops(builder);
        ConfigureCustomers(builder);
        ConfigureCategories(builder);
        ConfigureProducts(builder);
        ConfigureProductAttributes(builder);
        ConfigureCarts(builder);
        ConfigureOrders(builder);
        ConfigureProductEmbeddings(builder);
        ConfigureChat(builder);
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
            b.Property(x => x.PhoneNumber).IsRequired(false).HasMaxLength(32); // PhoneNumber is optional
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.NormalizedEmail).HasMaxLength(256);
            b.Property(x => x.UserName).HasMaxLength(256);
            b.Property(x => x.NormalizedUserName).HasMaxLength(256);
            b.Property(x => x.PasswordHash).HasMaxLength(256);
            b.Property(x => x.SecurityStamp).HasMaxLength(256);

            // Configure relationships
            // Note: IdentityUserClaim, IdentityUserLogin, IdentityUserToken relationships
            // are handled by the Identity system through the store implementation.
            // Only configure CustomerUserRole which uses a custom table.
            b.HasMany(x => x.Roles)
                .WithOne(ur => ur.Customer)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            // Indexes
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.Email);
            b.HasIndex(x => x.NormalizedEmail);
            b.HasIndex(x => x.UserName);
            b.HasIndex(x => x.NormalizedUserName);
            b.HasIndex(x => x.PhoneNumber);
        });

        builder.Entity<CustomerUserRole>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "CustomerUserRole", APMEConsts.DbSchema);
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.ConfigureByConvention();
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
            b.Property(x => x.PrimaryImageUrl).HasMaxLength(512);
            b.Property(x => x.ImageUrls).HasColumnType("jsonb");
            
            // Concurrency control for stock updates
            b.Property(x => x.StockConcurrencyStamp).IsRequired().HasMaxLength(40).IsConcurrencyToken();
            b.Property(x => x.LowStockThreshold).HasDefaultValue(10);

            // AI/Embedding support (RAG Architecture)
            b.Property(x => x.CanonicalDocument).HasColumnType("jsonb");
            b.Property(x => x.CanonicalDocumentVersion).HasDefaultValue(0);
            b.Property(x => x.EmbeddingGenerated).HasDefaultValue(false);

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
            // Index for finding products that need embedding generation
            b.HasIndex(x => new { x.IsActive, x.IsPublished, x.EmbeddingGenerated });
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
            
            // Embedding control properties for RAG/AI
            b.Property(x => x.IncludeInEmbedding).HasDefaultValue(true);
            b.Property(x => x.EmbeddingPriority).HasDefaultValue(0);
            b.Property(x => x.SemanticLabel).HasMaxLength(256);

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
            // Index for embedding-included attributes (for efficient filtering)
            b.HasIndex(x => new { x.ShopId, x.IncludeInEmbedding });
        });
    }

    private void ConfigureCarts(ModelBuilder builder)
    {
        builder.Entity<Cart>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Carts", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            // Cart is at host level - not tenant-specific
            // Customer can have items from multiple shops in a single cart

            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(1000);

            // Foreign keys
            b.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart items relationship
            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
            
            // Unique constraint: one active cart per customer (host level)
            b.HasIndex(x => new { x.CustomerId, x.Status })
                .HasFilter("\"Status\" = 0") // Active status
                .IsUnique();
        });

        builder.Entity<CartItem>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "CartItems", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            // ShopId on item allows multi-shop cart
            b.Property(x => x.ShopId).IsRequired();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ProductSku).IsRequired().HasMaxLength(128);
            b.Property(x => x.ProductImageUrl).HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

            // Note: No FK constraint to Shop because Cart is host-level (no tenant)
            // while Shop is multi-tenant. ShopId is stored as reference only.
            // Validation is handled in the application layer with tenant filter disabled.

            // Indexes
            b.HasIndex(x => x.CartId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.ProductId);
            
            // Unique constraint: one item per product per shop in a cart
            b.HasIndex(x => new { x.CartId, x.ShopId, x.ProductId }).IsUnique();
        });
    }

    private void ConfigureOrders(ModelBuilder builder)
    {
        builder.Entity<Order>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "Orders", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            // Order is at host level - not tenant-specific
            // Can contain items from multiple shops (ShopId is on OrderItem)

            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            b.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.ShippingAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.CustomerNotes).HasMaxLength(1000);
            b.Property(x => x.InternalNotes).HasMaxLength(1000);
            b.Property(x => x.TrackingNumber).HasMaxLength(100);
            b.Property(x => x.ShippingCarrier).HasMaxLength(100);
            b.Property(x => x.CancellationReason).HasMaxLength(500);

            // Shipping Address (owned entity / value object)
            b.OwnsOne(x => x.ShippingAddress, a =>
            {
                a.Property(p => p.FullName).HasColumnName("ShippingFullName").IsRequired().HasMaxLength(256);
                a.Property(p => p.Street).HasColumnName("ShippingStreet").IsRequired().HasMaxLength(256);
                a.Property(p => p.Street2).HasColumnName("ShippingStreet2").HasMaxLength(256);
                a.Property(p => p.City).HasColumnName("ShippingCity").IsRequired().HasMaxLength(128);
                a.Property(p => p.State).HasColumnName("ShippingState").HasMaxLength(128);
                a.Property(p => p.PostalCode).HasColumnName("ShippingPostalCode").IsRequired().HasMaxLength(20);
                a.Property(p => p.Country).HasColumnName("ShippingCountry").IsRequired().HasMaxLength(2);
                a.Property(p => p.Phone).HasColumnName("ShippingPhone").HasMaxLength(32);
            });

            // Billing Address (owned entity / value object, optional)
            b.OwnsOne(x => x.BillingAddress, a =>
            {
                a.Property(p => p.FullName).HasColumnName("BillingFullName").HasMaxLength(256);
                a.Property(p => p.Street).HasColumnName("BillingStreet").HasMaxLength(256);
                a.Property(p => p.Street2).HasColumnName("BillingStreet2").HasMaxLength(256);
                a.Property(p => p.City).HasColumnName("BillingCity").HasMaxLength(128);
                a.Property(p => p.State).HasColumnName("BillingState").HasMaxLength(128);
                a.Property(p => p.PostalCode).HasColumnName("BillingPostalCode").HasMaxLength(20);
                a.Property(p => p.Country).HasColumnName("BillingCountry").HasMaxLength(2);
                a.Property(p => p.Phone).HasColumnName("BillingPhone").HasMaxLength(32);
            });

            // Payment Snapshot (owned entity / value object)
            b.OwnsOne(x => x.Payment, p =>
            {
                p.Property(x => x.Method).HasColumnName("PaymentMethod").IsRequired();
                p.Property(x => x.TransactionId).HasColumnName("PaymentTransactionId").IsRequired().HasMaxLength(256);
                p.Property(x => x.Amount).HasColumnName("PaymentAmount").HasColumnType("decimal(18,2)");
                p.Property(x => x.Currency).HasColumnName("PaymentCurrency").IsRequired().HasMaxLength(3);
                p.Property(x => x.Status).HasColumnName("PaymentStatus").IsRequired();
                p.Property(x => x.ProcessedAt).HasColumnName("PaymentProcessedAt");
                p.Property(x => x.CardLast4).HasColumnName("PaymentCardLast4").HasMaxLength(4);
                p.Property(x => x.CardBrand).HasColumnName("PaymentCardBrand").HasMaxLength(20);
                p.Property(x => x.FailureMessage).HasColumnName("PaymentFailureMessage").HasMaxLength(500);
            });

            // Foreign keys
            b.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order items relationship
            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => new { x.CustomerId, x.CreationTime });
        });

        builder.Entity<OrderItem>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "OrderItems", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            // ShopId on item allows multi-shop orders
            b.Property(x => x.ShopId).IsRequired();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ProductSku).IsRequired().HasMaxLength(128);
            b.Property(x => x.ProductImageUrl).HasMaxLength(512);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

            // Note: No FK constraint to Shop because Order is host-level (no tenant)
            // while Shop is multi-tenant. ShopId is stored as reference only.
            // Validation is handled in the application layer with tenant filter disabled.

            // Indexes
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.ShopId);
            b.HasIndex(x => x.ProductId);
        });
    }

    /// <summary>
    /// Configures ProductEmbedding entity for AI/RAG vector storage.
    /// Uses pgvector extension with HNSW index for efficient similarity search.
    /// SRS Reference: AI Chatbot - Vector Storage & Indexing
    /// </summary>
    private void ConfigureProductEmbeddings(ModelBuilder builder)
    {
        builder.Entity<ProductEmbedding>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "ProductEmbeddings", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            // Primary key
            b.HasKey(x => x.Id);

            // Product reference (no FK - products may be deleted, embeddings cleaned up async)
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.TenantId);
            b.Property(x => x.ShopId).IsRequired();

            // Chunk information
            b.Property(x => x.ChunkIndex).IsRequired().HasDefaultValue(0);
            b.Property(x => x.ChunkText).IsRequired().HasMaxLength(8000);

            // Vector embedding - using pgvector type
            // Dimension 768 for embeddinggemma embeddings (adjust if using different model)
            b.Property(x => x.Embedding)
                .HasColumnType("vector(768)")
                .IsRequired();

            // Embedding metadata
            b.Property(x => x.EmbeddingVersion).IsRequired().HasDefaultValue(1);
            b.Property(x => x.EmbeddingModel).IsRequired().HasMaxLength(64);
            b.Property(x => x.CanonicalDocumentVersion).IsRequired().HasDefaultValue(1);
            b.Property(x => x.GeneratedAt).IsRequired();

            // Payload for quick context retrieval
            b.Property(x => x.PayloadJson).HasColumnType("jsonb");

            // Active flag for soft filtering
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            // Indexes

            // Unique constraint: one embedding per product per chunk
            b.HasIndex(x => new { x.ProductId, x.ChunkIndex })
                .IsUnique()
                .HasDatabaseName("IX_ProductEmbeddings_ProductId_ChunkIndex");

            // Index for tenant-scoped queries
            b.HasIndex(x => x.TenantId)
                .HasDatabaseName("IX_ProductEmbeddings_TenantId");

            // Index for shop-scoped queries
            b.HasIndex(x => x.ShopId)
                .HasDatabaseName("IX_ProductEmbeddings_ShopId");

            // Index for active embeddings (filtered search)
            b.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_ProductEmbeddings_IsActive");

            // Composite index for common query pattern
            b.HasIndex(x => new { x.IsActive, x.TenantId })
                .HasDatabaseName("IX_ProductEmbeddings_IsActive_TenantId");

            // HNSW index for vector similarity search (cosine distance)
            // This is the primary index for semantic search
            b.HasIndex(x => x.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops")
                .HasDatabaseName("IX_ProductEmbeddings_Embedding_HNSW");
        });
    }

    private void ConfigureChat(ModelBuilder builder)
    {
        builder.Entity<ChatSession>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "ChatSessions", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.LastActivityAt).IsRequired();
            b.Property(x => x.Title).HasMaxLength(256);
            b.Property(x => x.Metadata).HasColumnType("jsonb");

            // Foreign key to Customer
            b.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.LastActivityAt);
            b.HasIndex(x => new { x.CustomerId, x.Status });
            b.HasIndex(x => new { x.CustomerId, x.LastActivityAt });
        });

        builder.Entity<ChatMessage>(b =>
        {
            b.ToTable(APMEConsts.DbTablePrefix + "ChatMessages", APMEConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.SessionId).IsRequired();
            b.Property(x => x.SequenceNumber).IsRequired();
            b.Property(x => x.Role).IsRequired();
            b.Property(x => x.Content).IsRequired().HasMaxLength(8000);
            b.Property(x => x.IsArchived).IsRequired().HasDefaultValue(false);
            b.Property(x => x.Metadata).HasColumnType("jsonb");

            // Foreign key to ChatSession
            b.HasOne<ChatSession>()
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.IsArchived);
            b.HasIndex(x => x.CreationTime);
            // Unique constraint: one message per sequence number per session
            b.HasIndex(x => new { x.SessionId, x.SequenceNumber })
                .IsUnique()
                .HasDatabaseName("IX_ChatMessages_SessionId_SequenceNumber");
            // Index for efficient retrieval of recent messages
            b.HasIndex(x => new { x.SessionId, x.CreationTime });
            // Index for archival queries
            b.HasIndex(x => new { x.IsArchived, x.CreationTime });
        });
    }
}

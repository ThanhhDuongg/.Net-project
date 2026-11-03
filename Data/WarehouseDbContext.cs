using Microsoft.EntityFrameworkCore;
using NetProject.Models.Entities;
using NetProject.Models.Enums;

namespace NetProject.Data;

/// <summary>Cung cấp người dùng hiện tại cho các thuộc tính audit bóng (shadow properties).</summary>
public interface IAuditUserContext
{
    string Username { get; }
}

public sealed class SystemAuditUserContext : IAuditUserContext
{
    public string Username => "system";
}

/// <summary>
/// EF Core DbContext cho Fashion Warehouse.
/// Audit được lưu dưới dạng shadow properties: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy;
/// các thuộc tính này không xuất hiện trong entity domain và tự cập nhật khi SaveChanges.
/// </summary>
public sealed class WarehouseDbContext : DbContext
{
    private readonly IAuditUserContext _auditUserContext;

    public WarehouseDbContext(
        DbContextOptions<WarehouseDbContext> options,
        IAuditUserContext auditUserContext) : base(options)
    {
        _auditUserContext = auditUserContext;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptDetail> ReceiptDetails => Set<ReceiptDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Color>().ToTable("Colors");
        modelBuilder.Entity<Size>().ToTable("Sizes");
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<ProductVariant>().ToTable("ProductVariants");
        modelBuilder.Entity<WarehouseLocation>().ToTable("WarehouseLocations");
        modelBuilder.Entity<Inventory>().ToTable("Inventories");
        modelBuilder.Entity<InventoryTransaction>().ToTable("InventoryTransactions");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<Receipt>().ToTable("Receipts");
        modelBuilder.Entity<ReceiptDetail>().ToTable("ReceiptDetails");

        modelBuilder.Entity<Product>().HasIndex(x => x.ProductCode).IsUnique();
        modelBuilder.Entity<ProductVariant>().HasIndex(x => x.SKU).IsUnique();
        modelBuilder.Entity<ProductVariant>().HasIndex(x => x.Barcode).IsUnique();
        modelBuilder.Entity<ProductVariant>().HasIndex(x => new { x.ProductId, x.ColorId, x.SizeId }).IsUnique();
        modelBuilder.Entity<WarehouseLocation>().HasIndex(x => x.LocationCode).IsUnique();
        modelBuilder.Entity<Inventory>().HasIndex(x => new { x.VariantId, x.LocationId }).IsUnique();
        modelBuilder.Entity<Receipt>().HasIndex(x => x.ReceiptCode).IsUnique();
        modelBuilder.Entity<Receipt>().HasIndex(x => new { x.ReceiptDate, x.ReceiptType });
        modelBuilder.Entity<InventoryTransaction>().HasIndex(x => new { x.VariantId, x.LocationId, x.OccurredAt });
        modelBuilder.Entity<InventoryTransaction>().HasIndex(x => x.ReferenceNo);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Variant)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inventory>()
            .HasOne(x => x.Location)
            .WithMany(x => x.Inventories)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Receipt>()
            .Property(x => x.ReceiptType)
            .HasConversion<string>()
            .HasMaxLength(10);

        modelBuilder.Entity<Receipt>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(12);

        modelBuilder.Entity<InventoryTransaction>()
            .Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(12);

        modelBuilder.Entity<ReceiptDetail>()
            .Property(x => x.TotalLineAmount)
            .HasComputedColumnSql("Quantity * UnitPrice", stored: true);

        // DATETIME(6) đổi mỗi UPDATE của MySQL; EF dùng giá trị cũ trong WHERE để phát hiện xung đột.
        modelBuilder.Entity<Inventory>()
            .Property(x => x.RowVersion)
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        ConfigureAuditShadowProperties(modelBuilder.Entity<Product>());
        ConfigureAuditShadowProperties(modelBuilder.Entity<ProductVariant>());
        ConfigureAuditShadowProperties(modelBuilder.Entity<Inventory>());
        ConfigureAuditShadowProperties(modelBuilder.Entity<InventoryTransaction>());
        ConfigureAuditShadowProperties(modelBuilder.Entity<Receipt>());
        ConfigureAuditShadowProperties(modelBuilder.Entity<ReceiptDetail>());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static void ConfigureAuditShadowProperties<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        entity.Property<DateTime>("CreatedAt")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .IsRequired();
        entity.Property<string>("CreatedBy").HasMaxLength(100);
        entity.Property<DateTime?>("UpdatedAt").HasColumnType("datetime(6)");
        entity.Property<string>("UpdatedBy").HasMaxLength(100);
    }

    private void ApplyAuditInformation()
    {
        var now = DateTime.UtcNow;
        var username = string.IsNullOrWhiteSpace(_auditUserContext.Username)
            ? "system"
            : _auditUserContext.Username.Trim();

        foreach (var entry in ChangeTracker.Entries().Where(x => x.Metadata.FindProperty("CreatedAt") is not null))
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("CreatedBy").CurrentValue = username;
                entry.Property("UpdatedAt").CurrentValue = now;
                entry.Property("UpdatedBy").CurrentValue = username;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("CreatedAt").IsModified = false;
                entry.Property("CreatedBy").IsModified = false;
                entry.Property("UpdatedAt").CurrentValue = now;
                entry.Property("UpdatedBy").CurrentValue = username;
            }
        }
    }
}

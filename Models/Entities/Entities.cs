using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetProject.Models.Enums;

namespace NetProject.Models.Entities;

public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Color
{
    [Key]
    public int ColorId { get; set; }

    [Required, MaxLength(50)]
    public string ColorName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? ColorCode { get; set; }

    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

public class Size
{
    [Key]
    public int SizeId { get; set; }

    [Required, MaxLength(20)]
    public string SizeName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required, MaxLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "Cái";

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}

public class WarehouseLocation
{
    [Key]
    public int LocationId { get; set; }

    [Required, MaxLength(30)]
    public string LocationCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LocationName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}

/// <summary>
/// Số lượng tồn tại một biến thể hàng hóa ở một vị trí kho.
/// RowVersion là token kiểm soát đồng thời lạc quan: mọi UPDATE đều phải so khớp bản gốc.
/// </summary>
public class Inventory
{
    [Key]
    public int InventoryId { get; set; }

    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public int QuantityOnHand { get; set; }
    public int SafetyStock { get; set; } = 10;
    public DateTime? LastImportDate { get; set; }

    [ConcurrencyCheck]
    public DateTime RowVersion { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

/// <summary>
/// Nhật ký bất biến cho mọi lần nhập, xuất hoặc điều chỉnh tồn kho.
/// </summary>
public class InventoryTransaction
{
    [Key]
    public long InventoryTransactionId { get; set; }

    public int InventoryId { get; set; }
    public Inventory Inventory { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;

    public int? ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public InventoryTransactionType TransactionType { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }

    [MaxLength(100)]
    public string ReferenceNo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int PerformedByUserId { get; set; }
}

public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required, MaxLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string CustomerType { get; set; } = "Đại lý cấp 1";

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DebtAmount { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [Required, MaxLength(150)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactName { get; set; }
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(255)]
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}

public class Receipt
{
    [Key]
    public int ReceiptId { get; set; }

    [Required, MaxLength(30)]
    public string ReceiptCode { get; set; } = string.Empty;

    public ReceiptType ReceiptType { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    [MaxLength(100)]
    public string? CustomerName { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
    public ReceiptStatus Status { get; set; } = ReceiptStatus.COMPLETED;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<ReceiptDetail> Details { get; set; } = new List<ReceiptDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}

public class ReceiptDetail
{
    [Key]
    public int DetailId { get; set; }
    public int ReceiptId { get; set; }
    public Receipt Receipt { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int LocationId { get; set; }
    public WarehouseLocation Location { get; set; } = null!;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLineAmount { get; set; }
}

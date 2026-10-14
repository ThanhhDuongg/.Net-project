using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetProject.Models.Enums;

namespace NetProject.Models.Entities;

public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Color
{
    [Key]
    public int ColorId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ColorName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? ColorCode { get; set; }

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

public class Size
{
    [Key]
    public int SizeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SizeName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "Cái";

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public int ColorId { get; set; }
    public virtual Color Color { get; set; } = null!;

    public int SizeId { get; set; }
    public virtual Size Size { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
}

public class WarehouseLocation
{
    [Key]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(30)]
    public string LocationCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LocationName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
}

public class Inventory
{
    [Key]
    public int InventoryId { get; set; }

    public int VariantId { get; set; }
    public virtual ProductVariant Variant { get; set; } = null!;

    public int LocationId { get; set; }
    public virtual WarehouseLocation Location { get; set; } = null!;

    public int QuantityOnHand { get; set; } = 0;
    public int SafetyStock { get; set; } = 10;
    public DateTime? LastImportDate { get; set; }

    // Concurrency Token: MySQL tự động cập nhật vi giây
    [ConcurrencyCheck]
    public DateTime RowVersion { get; set; }
}

public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [Required]
    [MaxLength(150)]
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

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}

public class Receipt
{
    [Key]
    public int ReceiptId { get; set; }

    [Required]
    [MaxLength(30)]
    public string ReceiptCode { get; set; } = string.Empty;

    public ReceiptType ReceiptType { get; set; } = ReceiptType.IMPORT;

    public DateTime ReceiptDate { get; set; } = DateTime.Now;

    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }

    [MaxLength(100)]
    public string? CustomerName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ReceiptStatus Status { get; set; } = ReceiptStatus.COMPLETED;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public virtual ICollection<ReceiptDetail> Details { get; set; } = new List<ReceiptDetail>();
}

public class ReceiptDetail
{
    [Key]
    public int DetailId { get; set; }

    public int ReceiptId { get; set; }
    public virtual Receipt Receipt { get; set; } = null!;

    public int VariantId { get; set; }
    public virtual ProductVariant Variant { get; set; } = null!;

    public int LocationId { get; set; }
    public virtual WarehouseLocation Location { get; set; } = null!;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLineAmount { get; set; }
}

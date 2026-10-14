using System;
using System.Collections.Generic;
using NetProject.Models.Enums;

namespace NetProject.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class CreateProductMatrixDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
    public string Unit { get; set; } = "Cái";
    public string? Description { get; set; }
    public List<int> ColorIds { get; set; } = new();
    public List<int> SizeIds { get; set; } = new();
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
}

public class GeneratedVariantDto
{
    public int ProductId { get; set; }
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int SizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
}

public class ReceiptItemDto
{
    public int VariantId { get; set; }
    public int LocationId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateReceiptDto
{
    public string ReceiptCode { get; set; } = string.Empty;
    public ReceiptType ReceiptType { get; set; }
    public int? SupplierId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
}

public class InventoryStatusDto
{
    public int VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int SafetyStock { get; set; }
    public bool IsLowStock => QuantityOnHand <= SafetyStock;
}

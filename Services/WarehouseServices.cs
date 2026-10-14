using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NetProject.DTOs;
using NetProject.Models.Entities;

namespace NetProject.Services;

// 1. Password Security Helper (Hỗ trợ băm mật khẩu chuẩn SHA256 / Salt)
public static class SecurityHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        string hash = HashPassword(inputPassword);
        return hash == storedHash;
    }
}

// 2. Barcode & SKU Generator Service
public class BarcodeGeneratorService
{
    // Sinh mã vạch chuẩn EAN/Code-128 12 ký tự số từ SKU
    public string GenerateBarcode(string sku)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sku));
        long number = Math.Abs(BitConverter.ToInt64(hash, 0)) % 100000000000L;
        return $"893{number:D9}";
    }

    // Sinh danh sách ma trận biến thể ProductVariant
    public List<GeneratedVariantDto> GenerateMatrix(
        string productCode,
        List<(int Id, string Name)> colors,
        List<(int Id, string Name)> sizes,
        decimal costPrice,
        decimal salePrice)
    {
        var result = new List<GeneratedVariantDto>();

        foreach (var c in colors)
        {
            foreach (var s in sizes)
            {
                // Quy tắc SKU: MÃSẢNPHẨM-MÃMÀU-SIZE (VD: POLO01-BLK-M)
                string colorAbbr = c.Name.Length >= 3 ? c.Name[..3].ToUpper() : c.Name.ToUpper();
                string sku = $"{productCode}-{colorAbbr}-{s.Name.ToUpper()}";
                string barcode = GenerateBarcode(sku);

                result.Add(new GeneratedVariantDto
                {
                    ColorId = c.Id,
                    ColorName = c.Name,
                    SizeId = s.Id,
                    SizeName = s.Name,
                    SKU = sku,
                    Barcode = barcode,
                    CostPrice = costPrice,
                    SalePrice = salePrice
                });
            }
        }

        return result;
    }
}

using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;

namespace NetProject.Services;

public interface IProductService
{
    Task<List<ProductListItemDto>> GetAllProductsAsync(CancellationToken ct = default);
    Task<ProductDetailDto> CreateProductWithVariantsAsync(CreateProductMatrixDto dto, CancellationToken ct = default);
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(int id, string name, string? description, CancellationToken ct = default);
    Task DeleteCategoryAsync(int id, CancellationToken ct = default);
}

public sealed class ProductService : IProductService
{
    private readonly WarehouseDbContext _context;
    private readonly BarcodeGeneratorService _barcodeService;

    public ProductService(WarehouseDbContext context, BarcodeGeneratorService barcodeService)
    {
        _context = context;
        _barcodeService = barcodeService;
    }

    public async Task<List<ProductListItemDto>> GetAllProductsAsync(CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Where(p => p.IsActive)
            .Select(p => new ProductListItemDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                CategoryName = p.Category.CategoryName,
                BasePrice = p.BasePrice,
                Unit = p.Unit,
                VariantCount = p.Variants.Count(v => v.IsActive)
            })
            .OrderBy(p => p.ProductCode)
            .ToListAsync(ct);
    }

    public async Task<ProductDetailDto> CreateProductWithVariantsAsync(CreateProductMatrixDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductCode))
            throw new ArgumentException("Mã sản phẩm không được để trống.");

        if (string.IsNullOrWhiteSpace(dto.ProductName))
            throw new ArgumentException("Tên sản phẩm không được để trống.");

        if (dto.ColorIds.Count == 0)
            throw new ArgumentException("Phải chọn ít nhất một màu sắc.");

        if (dto.SizeIds.Count == 0)
            throw new ArgumentException("Phải chọn ít nhất một kích cỡ.");

        var colors = await _context.Colors
            .Where(c => dto.ColorIds.Contains(c.ColorId))
            .ToListAsync(ct);

        var sizes = await _context.Sizes
            .Where(s => dto.SizeIds.Contains(s.SizeId))
            .ToListAsync(ct);

        var firstLocation = await _context.WarehouseLocations.OrderBy(l => l.LocationId).FirstOrDefaultAsync(ct);
        int defaultLocationId = firstLocation?.LocationId ?? 1;

        var colorTuples = colors.Select(c => (c.ColorId, c.ColorName)).ToList();
        var sizeTuples = sizes.Select(s => (s.SizeId, s.SizeName)).ToList();

        List<GeneratedVariantDto> matrix;
        if (dto.Variants != null && dto.Variants.Count > 0)
        {
            matrix = dto.Variants.Select(v =>
            {
                var cName = colors.FirstOrDefault(c => c.ColorId == v.ColorId)?.ColorName ?? "";
                var sName = sizes.FirstOrDefault(s => s.SizeId == v.SizeId)?.SizeName ?? "";
                var sku = !string.IsNullOrWhiteSpace(v.SKU)
                    ? v.SKU.Trim()
                    : $"{dto.ProductCode.Trim()}-{BarcodeGeneratorService.GetColorAbbreviation(cName)}-{sName.ToUpper()}";
                var barcode = !string.IsNullOrWhiteSpace(v.Barcode)
                    ? v.Barcode.Trim()
                    : _barcodeService.GenerateBarcode(sku);

                return new GeneratedVariantDto
                {
                    ColorId = v.ColorId,
                    ColorName = cName,
                    SizeId = v.SizeId,
                    SizeName = sName,
                    SKU = sku,
                    Barcode = barcode,
                    CostPrice = v.CostPrice > 0 ? v.CostPrice : dto.CostPrice,
                    SalePrice = v.SalePrice > 0 ? v.SalePrice : dto.SalePrice
                };
            }).ToList();
        }
        else
        {
            matrix = _barcodeService.GenerateMatrix(
                dto.ProductCode.Trim(), colorTuples, sizeTuples, dto.CostPrice, dto.SalePrice);
        }

        var existingProduct = await _context.Products
            .Include(p => p.Variants).ThenInclude(v => v.Inventories)
            .FirstOrDefaultAsync(p => p.ProductCode == dto.ProductCode.Trim(), ct);

        if (existingProduct != null)
        {
            // Bổ sung các biến thể ma trận mới cho sản phẩm đã tồn tại
            existingProduct.ProductName = dto.ProductName.Trim();
            existingProduct.CategoryId = dto.CategoryId;
            existingProduct.BasePrice = dto.BasePrice;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                existingProduct.Description = dto.Description.Trim();

            foreach (var item in matrix)
            {
                var existingVar = existingProduct.Variants.FirstOrDefault(v => v.SKU == item.SKU);
                if (existingVar != null)
                {
                    existingVar.Barcode = item.Barcode;
                    existingVar.CostPrice = item.CostPrice;
                    existingVar.SalePrice = item.SalePrice;
                    existingVar.IsActive = true;
                }
                else
                {
                    var newVar = new ProductVariant
                    {
                        ColorId = item.ColorId,
                        SizeId = item.SizeId,
                        SKU = item.SKU,
                        Barcode = item.Barcode,
                        CostPrice = item.CostPrice,
                        SalePrice = item.SalePrice,
                        IsActive = true
                    };
                    newVar.Inventories.Add(new Inventory
                    {
                        LocationId = defaultLocationId,
                        QuantityOnHand = 0,
                        SafetyStock = 10,
                        RowVersion = DateTime.UtcNow
                    });
                    existingProduct.Variants.Add(newVar);
                }
            }

            await _context.SaveChangesAsync(ct);

            return new ProductDetailDto
            {
                ProductId = existingProduct.ProductId,
                ProductCode = existingProduct.ProductCode,
                ProductName = existingProduct.ProductName,
                Variants = matrix
            };
        }

        var product = new Product
        {
            ProductCode = dto.ProductCode.Trim(),
            ProductName = dto.ProductName.Trim(),
            CategoryId = dto.CategoryId,
            BasePrice = dto.BasePrice,
            Unit = dto.Unit,
            Description = dto.Description?.Trim()
        };

        foreach (var item in matrix)
        {
            var variant = new ProductVariant
            {
                ColorId = item.ColorId,
                SizeId = item.SizeId,
                SKU = item.SKU,
                Barcode = item.Barcode,
                CostPrice = item.CostPrice,
                SalePrice = item.SalePrice,
                IsActive = true
            };
            variant.Inventories.Add(new Inventory
            {
                LocationId = defaultLocationId,
                QuantityOnHand = 0,
                SafetyStock = 10,
                RowVersion = DateTime.UtcNow
            });
            product.Variants.Add(variant);
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        return new ProductDetailDto
        {
            ProductId = product.ProductId,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Variants = matrix
        };
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name, string? description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var category = new Category
        {
            CategoryName = name.Trim(),
            Description = description?.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);

        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            ProductCount = 0
        };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var category = await _context.Categories.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Danh mục #{id} không tồn tại.");

        category.CategoryName = name.Trim();
        category.Description = description?.Trim();
        await _context.SaveChangesAsync(ct);

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == id && p.IsActive, ct);

        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            ProductCount = productCount
        };
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await _context.Categories.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Danh mục #{id} không tồn tại.");

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && p.IsActive, ct);
        if (hasProducts)
            throw new InvalidOperationException("Không thể xóa danh mục đang chứa sản phẩm.");

        category.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }
}

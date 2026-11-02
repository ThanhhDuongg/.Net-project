using Xunit;
using NetProject.Services;

namespace BTL_CKy.UnitTests;

public sealed class BarcodeGeneratorServiceTests
{
    private readonly BarcodeGeneratorService _service = new();

    [Fact]
    public void GenerateBarcode_ReturnsTwelveDigitString()
    {
        var barcode = _service.GenerateBarcode("POLO01-BLK-M");

        Assert.StartsWith("893", barcode);
        Assert.Equal(12, barcode.Length);
        Assert.True(barcode.All(char.IsDigit));
    }

    [Fact]
    public void GenerateBarcode_SameSku_ReturnsSameBarcode()
    {
        var barcode1 = _service.GenerateBarcode("SM-OXF-BLU-S");
        var barcode2 = _service.GenerateBarcode("SM-OXF-BLU-S");

        Assert.Equal(barcode1, barcode2);
    }

    [Fact]
    public void GenerateBarcode_DifferentSku_ReturnsDifferentBarcode()
    {
        var barcode1 = _service.GenerateBarcode("POLO01-BLK-M");
        var barcode2 = _service.GenerateBarcode("POLO01-WHT-L");

        Assert.NotEqual(barcode1, barcode2);
    }

    [Fact]
    public void GenerateMatrix_ProducesCorrectCartesianProduct()
    {
        var colors = new List<(int Id, string Name)> { (1, "Trắng"), (2, "Đen") };
        var sizes = new List<(int Id, string Name)> { (1, "S"), (2, "M"), (3, "L") };

        var result = _service.GenerateMatrix("POLO01", colors, sizes, 120000, 250000);

        Assert.Equal(6, result.Count);
    }

    [Fact]
    public void GenerateMatrix_SkuFollsNamingConvention()
    {
        var colors = new List<(int Id, string Name)> { (1, "Trắng") };
        var sizes = new List<(int Id, string Name)> { (2, "M") };

        var result = _service.GenerateMatrix("POLO01", colors, sizes, 120000, 250000);

        Assert.Single(result);
        Assert.Equal("POLO01-TRẮ-M", result[0].SKU);
    }

    [Fact]
    public void GenerateMatrix_EachVariantHasUniqueBarcode()
    {
        var colors = new List<(int Id, string Name)> { (1, "WHT"), (2, "BLK"), (3, "NAV") };
        var sizes = new List<(int Id, string Name)> { (1, "S"), (2, "M"), (3, "L"), (4, "XL") };

        var result = _service.GenerateMatrix("TESTPROD", colors, sizes, 100000, 200000);

        var barcodes = result.Select(v => v.Barcode).ToList();
        Assert.Equal(barcodes.Distinct().Count(), barcodes.Count);
    }

    [Fact]
    public void GenerateMatrix_PreservesPrices()
    {
        var colors = new List<(int Id, string Name)> { (1, "Đỏ") };
        var sizes = new List<(int Id, string Name)> { (1, "XL") };

        var result = _service.GenerateMatrix("AO01", colors, sizes, 99000, 199000);

        Assert.Single(result);
        Assert.Equal(99000m, result[0].CostPrice);
        Assert.Equal(199000m, result[0].SalePrice);
    }

    [Fact]
    public void GenerateMatrix_PreservesColorAndSizeIds()
    {
        var colors = new List<(int Id, string Name)> { (5, "Xanh"), (8, "Vàng") };
        var sizes = new List<(int Id, string Name)> { (3, "L") };

        var result = _service.GenerateMatrix("SP01", colors, sizes, 100000, 200000);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.ColorId == 5 && v.SizeId == 3);
        Assert.Contains(result, v => v.ColorId == 8 && v.SizeId == 3);
    }

    [Fact]
    public void GenerateMatrix_EmptyColors_ReturnsEmptyList()
    {
        var colors = new List<(int Id, string Name)>();
        var sizes = new List<(int Id, string Name)> { (1, "S") };

        var result = _service.GenerateMatrix("EMPTY", colors, sizes, 100000, 200000);

        Assert.Empty(result);
    }

    [Fact]
    public void GenerateMatrix_EmptySizes_ReturnsEmptyList()
    {
        var colors = new List<(int Id, string Name)> { (1, "Đen") };
        var sizes = new List<(int Id, string Name)>();

        var result = _service.GenerateMatrix("EMPTY", colors, sizes, 100000, 200000);

        Assert.Empty(result);
    }
}

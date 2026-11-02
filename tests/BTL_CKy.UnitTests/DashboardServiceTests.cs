using Moq;
using NetProject.DAL;
using NetProject.DTOs;
using NetProject.Services;
using Xunit;

namespace BTL_CKy.UnitTests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetInventoryStatusesAsync_AppliesSearchLocationAndLowStockFilters()
    {
        var reportRepository = new Mock<IReportRepository>();
        reportRepository.Setup(repository => repository.GetInventoryStatuses())
            .Returns(new[]
            {
                new InventoryStatusDto
                {
                    VariantId = 1,
                    SKU = "TEE-BLK-M",
                    Barcode = "893000000001",
                    ProductName = "Classic Tee",
                    ColorName = "Black",
                    LocationCode = "RACK-A1",
                    QuantityOnHand = 2,
                    SafetyStock = 5
                },
                new InventoryStatusDto
                {
                    VariantId = 2,
                    SKU = "TEE-WHT-M",
                    Barcode = "893000000002",
                    ProductName = "Classic Tee",
                    ColorName = "White",
                    LocationCode = "RACK-A1",
                    QuantityOnHand = 12,
                    SafetyStock = 5
                },
                new InventoryStatusDto
                {
                    VariantId = 3,
                    SKU = "JEAN-BLU-32",
                    Barcode = "893000000003",
                    ProductName = "Slim Jeans",
                    ColorName = "Blue",
                    LocationCode = "RACK-B1",
                    QuantityOnHand = 1,
                    SafetyStock = 4
                }
            }.AsQueryable());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.Reports).Returns(reportRepository.Object);
        var service = new DashboardService(unitOfWork.Object);

        var result = await service.GetInventoryStatusesAsync("TEE", "RACK-A1", lowStockOnly: true);

        var item = Assert.Single(result);
        Assert.Equal(1, item.VariantId);
        Assert.Equal("TEE-BLK-M", item.SKU);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public async Task GetInventoryStatusesAsync_RejectsOutOfRangeTake(int take)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new DashboardService(unitOfWork.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetInventoryStatusesAsync(take: take));
    }
}

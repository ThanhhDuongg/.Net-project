using Xunit;
using Moq;
using NetProject.DAL;
using NetProject.DTOs;
using NetProject.Models.Entities;
using NetProject.Models.Enums;
using NetProject.Services;
using NetProject.Services.Exceptions;

namespace BTL_CKy.UnitTests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task Export_WhenStockIsInsufficient_DoesNotSubtractAndRollsBack()
    {
        var inventory = CreateInventory(quantity: 3);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var transactions = new Mock<IInventoryTransactionRepository>();
        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        var service = new InventoryService(uow.Object);

        var request = CreateExportRequest(quantity: 5);

        await Assert.ThrowsAsync<InsufficientInventoryException>(() => service.ProcessAsync(request));

        Assert.Equal(3, inventory.QuantityOnHand);
        transactions.Verify(x => x.Add(It.IsAny<InventoryTransaction>()), Times.Never);
        receipts.Verify(x => x.Add(It.IsAny<Receipt>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Export_WhenSaveFails_RestoresQuantityOnHandAndRollsBack()
    {
        var inventory = CreateInventory(quantity: 10);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var transactions = new Mock<IInventoryTransactionRepository>();
        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection terminated"));

        var service = new InventoryService(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(CreateExportRequest(quantity: 4)));

        // Tồn kho in-memory được phục hồi về giá trị trước thay đổi (10), transaction được rollback.
        Assert.Equal(10, inventory.QuantityOnHand);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Export_WhenVersionIsStale_DoesNotSubtractAndSignalsConcurrencyConflict()
    {
        var inventory = CreateInventory(quantity: 10);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var transactions = new Mock<IInventoryTransactionRepository>();
        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        var service = new InventoryService(uow.Object);
        var request = CreateExportRequest(quantity: 2);
        request.Items[0].ExpectedRowVersion = inventory.RowVersion.AddTicks(-1);

        await Assert.ThrowsAsync<InventoryConcurrencyException>(() => service.ProcessAsync(request));

        Assert.Equal(10, inventory.QuantityOnHand);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Export_WhenDbConcurrencyConflictOccurs_RestoresQuantityOnHandAndRollsBack()
    {
        var inventory = CreateInventory(quantity: 20);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var transactions = new Mock<IInventoryTransactionRepository>();
        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InventoryConcurrencyException(inventory.InventoryId));

        var service = new InventoryService(uow.Object);
        var request = CreateExportRequest(quantity: 5);

        await Assert.ThrowsAsync<InventoryConcurrencyException>(() => service.ProcessAsync(request));

        // Kiểm tra tồn kho phục hồi không bị trừ sai
        Assert.Equal(20, inventory.QuantityOnHand);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Export_WhenSuccessful_DeductsStock_AddsTransactionAndReceipt_AndCommits()
    {
        var inventory = CreateInventory(quantity: 15);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        InventoryTransaction? addedTxn = null;
        var transactions = new Mock<IInventoryTransactionRepository>();
        transactions.Setup(x => x.Add(It.IsAny<InventoryTransaction>()))
            .Callback<InventoryTransaction>(t => addedTxn = t);

        Receipt? addedReceipt = null;
        var receipts = new Mock<IReceiptRepository>();
        receipts.Setup(x => x.Add(It.IsAny<Receipt>()))
            .Callback<Receipt>(r => addedReceipt = r);

        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        var service = new InventoryService(uow.Object);

        var request = CreateExportRequest(quantity: 5);
        var result = await service.ProcessAsync(request);

        Assert.Equal(10, inventory.QuantityOnHand);
        Assert.NotNull(addedTxn);
        Assert.Equal(-5, addedTxn.QuantityChange);
        Assert.Equal(15, addedTxn.QuantityBefore);
        Assert.Equal(10, addedTxn.QuantityAfter);
        Assert.Equal(InventoryTransactionType.EXPORT, addedTxn.TransactionType);

        Assert.NotNull(addedReceipt);
        Assert.Equal("PX-20261016-001", addedReceipt.ReceiptCode);
        Assert.Equal(ReceiptType.EXPORT, addedReceipt.ReceiptType);

        Assert.Single(result.Balances);
        Assert.Equal(10, result.Balances[0].QuantityOnHand);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Import_WhenSuccessful_IncreasesStock_UpdatesLastImportDate_AndCommits()
    {
        var inventory = CreateInventory(quantity: 10);
        var inventoryRepository = new Mock<IInventoryRepository>();
        inventoryRepository
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        InventoryTransaction? addedTxn = null;
        var transactions = new Mock<IInventoryTransactionRepository>();
        transactions.Setup(x => x.Add(It.IsAny<InventoryTransaction>()))
            .Callback<InventoryTransaction>(t => addedTxn = t);

        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepository, transactions, receipts, reports);
        var service = new InventoryService(uow.Object);

        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "PN-20261016-001",
            TransactionType = ReceiptType.IMPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>
            {
                new() { VariantId = 1, LocationId = 1, Quantity = 8, UnitPrice = 100_000 }
            }
        };

        var result = await service.ProcessAsync(request);

        Assert.Equal(18, inventory.QuantityOnHand);
        Assert.NotNull(inventory.LastImportDate);
        Assert.NotNull(addedTxn);
        Assert.Equal(8, addedTxn.QuantityChange);
        Assert.Equal(10, addedTxn.QuantityBefore);
        Assert.Equal(18, addedTxn.QuantityAfter);
        Assert.Equal(InventoryTransactionType.IMPORT, addedTxn.TransactionType);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Export_WhenMultipleItemsSameVariant_GroupsAndDeductsCorrectly()
    {
        var inventory = CreateInventory(quantity: 10);
        var inventoryRepo = new Mock<IInventoryRepository>();
        inventoryRepo
            .Setup(x => x.FindByVariantAndLocationAsync(1, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var transactions = new Mock<IInventoryTransactionRepository>();
        var receipts = new Mock<IReceiptRepository>();
        var reports = new Mock<IReportRepository>();
        var uow = BuildUnitOfWork(inventoryRepo, transactions, receipts, reports);
        var service = new InventoryService(uow.Object);

        // Phiếu xuất có 2 dòng cùng Variant 1 và Location 1: dòng 1 xuất 3, dòng 2 xuất 4 => tổng trừ 7
        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "PX-20261016-MULTI",
            TransactionType = ReceiptType.EXPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>
            {
                new() { VariantId = 1, LocationId = 1, Quantity = 3, UnitPrice = 100_000 },
                new() { VariantId = 1, LocationId = 1, Quantity = 4, UnitPrice = 100_000 }
            }
        };

        var result = await service.ProcessAsync(request);

        Assert.Equal(3, inventory.QuantityOnHand); // 10 - 7 = 3
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new InventoryService(uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessAsync(null!));
    }

    [Fact]
    public async Task Process_WhenTransactionCodeIsEmpty_ThrowsArgumentException()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new InventoryService(uow.Object);

        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "   ",
            TransactionType = ReceiptType.IMPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>
            {
                new() { VariantId = 1, LocationId = 1, Quantity = 5, UnitPrice = 50_000 }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(request));
    }

    [Fact]
    public async Task Process_WhenNoItems_ThrowsArgumentException()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new InventoryService(uow.Object);

        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "PN-TEST-EMPTY",
            TransactionType = ReceiptType.IMPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>()
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(request));
    }

    [Fact]
    public async Task Process_WhenItemHasInvalidQuantityOrPrice_ThrowsArgumentException()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new InventoryService(uow.Object);

        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "PN-TEST-INVALID",
            TransactionType = ReceiptType.IMPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>
            {
                new() { VariantId = 1, LocationId = 1, Quantity = -5, UnitPrice = 50_000 }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(request));
    }

    [Fact]
    public async Task Export_WhenInventoryNotFound_ThrowsInventoryNotFoundExceptionAndRollsBack()
    {
        var inventoryRepo = new Mock<IInventoryRepository>();
        inventoryRepo
            .Setup(x => x.FindByVariantAndLocationAsync(999, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var uow = BuildUnitOfWork(inventoryRepo, new Mock<IInventoryTransactionRepository>(), new Mock<IReceiptRepository>(), new Mock<IReportRepository>());
        var service = new InventoryService(uow.Object);

        var request = new InventoryTransactionRequestDto
        {
            TransactionCode = "PX-NOTFOUND",
            TransactionType = ReceiptType.EXPORT,
            UserId = 1,
            Items = new List<InventoryTransactionItemDto>
            {
                new() { VariantId = 999, LocationId = 1, Quantity = 2, UnitPrice = 100_000 }
            }
        };

        await Assert.ThrowsAsync<InventoryNotFoundException>(() => service.ProcessAsync(request));
        uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindByBarcode_WhenBarcodeExists_ReturnsInventoryStatus()
    {
        var reports = new Mock<IReportRepository>();
        var sampleList = new List<InventoryStatusDto>
        {
            new() { Barcode = "893600100101", SKU = "POLO01-BLK-M", QuantityOnHand = 25, LocationCode = "KHO-A-01" },
            new() { Barcode = "893600100102", SKU = "POLO01-BLK-L", QuantityOnHand = 10, LocationCode = "KHO-A-01" }
        }.AsQueryable();

        reports.Setup(x => x.GetInventoryStatuses()).Returns(sampleList);

        var uow = BuildUnitOfWork(new Mock<IInventoryRepository>(), new Mock<IInventoryTransactionRepository>(), new Mock<IReceiptRepository>(), reports);
        var service = new InventoryService(uow.Object);

        var result = await service.FindByBarcodeAsync("893600100101");

        Assert.NotNull(result);
        Assert.Equal("POLO01-BLK-M", result.SKU);
        Assert.Equal(25, result.QuantityOnHand);
    }

    [Fact]
    public async Task FindByBarcode_WhenBarcodeIsEmpty_ReturnsNull()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new InventoryService(uow.Object);

        var result = await service.FindByBarcodeAsync("");
        Assert.Null(result);

        result = await service.FindByBarcodeAsync("   ");
        Assert.Null(result);
    }

    private static Mock<IUnitOfWork> BuildUnitOfWork(
        Mock<IInventoryRepository> inventoryRepository,
        Mock<IInventoryTransactionRepository> transactions,
        Mock<IReceiptRepository> receipts,
        Mock<IReportRepository> reports)
    {
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Inventories).Returns(inventoryRepository.Object);
        uow.SetupGet(x => x.InventoryTransactions).Returns(transactions.Object);
        uow.SetupGet(x => x.Receipts).Returns(receipts.Object);
        uow.SetupGet(x => x.Reports).Returns(reports.Object);
        uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return uow;
    }

    private static Inventory CreateInventory(int quantity) => new()
    {
        InventoryId = 9,
        VariantId = 1,
        LocationId = 1,
        QuantityOnHand = quantity,
        RowVersion = new DateTime(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc),
        Variant = new ProductVariant { SKU = "POLO01-BLK-M" }
    };

    private static InventoryTransactionRequestDto CreateExportRequest(int quantity) => new()
    {
        TransactionCode = "PX-20261016-001",
        TransactionType = ReceiptType.EXPORT,
        UserId = 1,
        Items = new List<InventoryTransactionItemDto>
        {
            new() { VariantId = 1, LocationId = 1, Quantity = quantity, UnitPrice = 120_000 }
        }
    };
}
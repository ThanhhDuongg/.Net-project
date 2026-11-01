using ClosedXML.Excel;
using NetProject.DAL;
using NetProject.DTOs;

namespace NetProject.Services.Reporting;

/// <summary>
/// Xuất báo cáo Excel bằng ClosedXML. Dữ liệu được stream từng batch từ IQueryable
/// thay vì tải toàn bộ bảng vào bộ nhớ.
/// </summary>
public interface IExcelReportService
{
    Task ExportInventoryReportAsync(Stream output, CancellationToken cancellationToken = default);
}

public sealed class ClosedXmlReportService : IExcelReportService
{
    private const int BatchSize = 5_000;
    private readonly IUnitOfWork _unitOfWork;

    public ClosedXmlReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExportInventoryReportAsync(Stream output, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        
        // Sheet 1: Báo cáo tồn kho hiện tại
        var sheet1 = workbook.Worksheets.Add("TonKho");
        WriteInventoryTitle(sheet1);
        WriteInventoryHeader(sheet1);

        var invQuery = _unitOfWork.Reports.GetInventoryReportRows();
        var row1 = 4;
        var count1 = 0;

        foreach (var batch in QueryInBatches(invQuery, BatchSize))
        {
            foreach (var item in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = sheet1.Row(row1);
                current.Cell(1).Value = item.SKU;
                current.Cell(2).Value = item.Barcode;
                current.Cell(3).Value = item.ProductName;
                current.Cell(4).Value = item.ColorName;
                current.Cell(5).Value = item.SizeName;
                current.Cell(6).Value = item.LocationCode;
                current.Cell(7).Value = item.QuantityOnHand;
                current.Cell(8).Value = item.SafetyStock;
                current.Cell(9).Value = item.CostPrice;
                current.Cell(10).Value = item.InventoryValue;
                row1++;
                count1++;
            }

            await Task.Yield();
        }

        FormatInventoryTable(sheet1, row1 - 1, count1);
        sheet1.Columns().AdjustToContents(1, Math.Min(row1 - 1, 200));

        // Sheet 2: Lịch sử biến động giao dịch kho (Audit Log)
        var sheet2 = workbook.Worksheets.Add("LichSuGiaoDich");
        WriteHistoryTitle(sheet2);
        WriteHistoryHeader(sheet2);

        var historyQuery = _unitOfWork.Reports.GetTransactionHistory(null, null);
        var row2 = 4;
        var count2 = 0;

        foreach (var batch in QueryInBatches(historyQuery, BatchSize))
        {
            foreach (var item in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = sheet2.Row(row2);
                current.Cell(1).Value = count2 + 1;
                current.Cell(2).Value = item.TransactionType;
                current.Cell(3).Value = item.ReferenceNo;
                current.Cell(4).Value = item.SKU;
                current.Cell(5).Value = item.ProductName;
                current.Cell(6).Value = item.ColorName;
                current.Cell(7).Value = item.SizeName;
                current.Cell(8).Value = item.LocationCode;
                current.Cell(9).Value = item.QuantityChange;
                current.Cell(10).Value = item.QuantityBefore;
                current.Cell(11).Value = item.QuantityAfter;
                current.Cell(12).Value = item.OccurredAt.ToString("dd/MM/yyyy HH:mm:ss");
                current.Cell(13).Value = item.Notes ?? string.Empty;
                row2++;
                count2++;
            }

            await Task.Yield();
        }

        FormatHistoryTable(sheet2, row2 - 1);
        sheet2.Columns().AdjustToContents(1, Math.Min(row2 - 1, 200));

        workbook.SaveAs(output);
    }

    private static IEnumerable<IReadOnlyList<T>> QueryInBatches<T>(
        IQueryable<T> query,
        int batchSize)
    {
        for (var skip = 0; ; skip += batchSize)
        {
            var items = query.Skip(skip).Take(batchSize).ToList();
            if (items.Count == 0)
            {
                yield break;
            }

            yield return items;
            if (items.Count < batchSize)
            {
                yield break;
            }
        }
    }

    private static void WriteInventoryTitle(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "BÁO CÁO TỒN KHO THỜI TRANG";
        sheet.Cell(2, 1).Value = $"Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Range(1, 1, 1, 10).Merge().Style
            .Font.SetBold().Font.SetFontSize(16)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E293B"))
            .Font.SetFontColor(XLColor.White)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Range(2, 1, 2, 10).Merge().Style
            .Font.SetItalic().Font.SetFontSize(10)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
    }

    private static void WriteInventoryHeader(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "SKU", "Barcode", "Tên hàng", "Màu", "Size", "Vị trí",
            "Tồn kho", "Ngưỡng an toàn", "Giá vốn", "Giá trị tồn"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2563EB"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        sheet.SheetView.FreezeRows(3);
        sheet.Range(3, 1, 3, 10).SetAutoFilter();
    }

    private static void FormatInventoryTable(IXLWorksheet sheet, int lastRow, int count)
    {
        if (lastRow < 4)
        {
            return;
        }

        var range = sheet.Range(3, 1, lastRow, 10);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        range.SetAutoFilter();

        sheet.Range(4, 9, lastRow, 10).Style.NumberFormat.Format = "#,##0";
        sheet.Range(4, 7, lastRow, 8).Style.NumberFormat.Format = "#,##0";

        sheet.Cell(lastRow + 2, 6).Value = "Tổng cộng";
        sheet.Cell(lastRow + 2, 6).Style.Font.SetBold();
        sheet.Cell(lastRow + 2, 7).Value = count;
        sheet.Cell(lastRow + 2, 10).Value = $"=SUM(J4:J{lastRow})";
    }

    private static void WriteHistoryTitle(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "SỔ LỊCH SỬ GIAO DỊCH BIẾN ĐỘNG KHO (AUDIT LOG)";
        sheet.Cell(2, 1).Value = $"Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Range(1, 1, 1, 13).Merge().Style
            .Font.SetBold().Font.SetFontSize(16)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#0F172A"))
            .Font.SetFontColor(XLColor.White)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Range(2, 1, 2, 13).Merge().Style
            .Font.SetItalic().Font.SetFontSize(10)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
    }

    private static void WriteHistoryHeader(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "STT", "Loại GD", "Mã tham chiếu", "Mã SKU", "Tên sản phẩm",
            "Màu sắc", "Size", "Vị trí", "Biến động SL", "Tồn trước", "Tồn sau",
            "Thời điểm GD", "Ghi chú"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0D9488"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        sheet.SheetView.FreezeRows(3);
        sheet.Range(3, 1, 3, 13).SetAutoFilter();
    }

    private static void FormatHistoryTable(IXLWorksheet sheet, int lastRow)
    {
        if (lastRow < 4)
        {
            return;
        }

        var range = sheet.Range(3, 1, lastRow, 13);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        range.SetAutoFilter();

        sheet.Range(4, 9, lastRow, 11).Style.NumberFormat.Format = "#,##0";
    }
}

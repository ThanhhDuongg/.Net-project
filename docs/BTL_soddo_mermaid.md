# MÃ MERMAID CÁC SƠ ĐỒ TRONG BÁO CÁO BTL

Cách dùng: dán từng khối mã vào [mermaid.live](https://mermaid.live), Typora, VS Code (có extension Mermaid) hoặc draw.io (Arrange → Insert → Advanced → Mermaid), rồi xuất ảnh PNG đặt độ phân giải cao (khuyến nghị Scale ≥ 2x) để chèn vào báo cáo.

---

## Hình 1.1 – Lưu đồ nghiệp vụ quản lý kho hiện tại của cửa hàng

```mermaid
flowchart TD
    NCC["Nhà cung cấp / Xưởng may"] -->|"Giao hàng + phiếu giao"| THK["Thủ kho"]
    THK -->|"Đếm từng mặt hàng theo kích cỡ"| SOTAY["Sổ tay ghi chép"]
    SOTAY -->|"Cuối ngày, nhập tay"| XL[("Tệp Excel tồn kho theo mẫu mã")]
    KH["Khách sỉ / Đại lý"] -->|"Đặt hàng"| NV["Nhân viên bán hàng"]
    NV -->|"Ghi phiếu xuất giấy"| PHIEU["Sổ phiếu xuất"]
    PHIEU -->|"Cuối ngày, cộng dồn trừ"| XL
    XL -->|"Cuối tháng, in bảng tồn"| KK["Đi dò kệ, kiểm kê"]
    KK -->|"Số thực tế"| DC["Đối chiếu Excel - tìm chênh lệch"]
    DC -->|"Sửa tay"| XL
```

---

## Hình 2.1 – Sơ đồ kiến trúc tổng thể hệ thống

```mermaid
flowchart LR
    subgraph DESKTOP["Ứng dụng WPF (FashionWarehouse.WpfApp)"]
        VIEWS["Các View: Dashboard, Product,<br/>Receipt, Inventory, Reports,<br/>Category, Supplier, Customer, Employee"]
        SESSION["UserSession<br/>(phiên đăng nhập, vai trò)"]
    end
    subgraph PHONE["Điện thoại - trình duyệt nội bộ"]
        SCAN["Trang quét /scan<br/>camera + JavaScript"]
    end
    subgraph API["ASP.NET Core Web API (NetProject, .NET 10)"]
        CTL["Controllers: Auth, Product,<br/>InventoryTransactions, Receipts,<br/>Users, Suppliers, Customers, Locations"]
        SVC["Services: AuthService, ProductService,<br/>InventoryService, DashboardService,<br/>ClosedXmlReportService, BarcodeGeneratorService"]
        UOW["DAL: IUnitOfWork / EfUnitOfWork<br/>WarehouseDbContext (EF Core)"]
        HUB["BarcodeScannerHub<br/>/hub/barcode"]
    end
    DB[("MySQL<br/>FashionWarehouseDb")]
    VIEWS -->|"HTTP/JSON (REST)"| CTL
    CTL --> SVC
    SVC --> UOW
    UOW -->|"EF Core - MySQL Connector"| DB
    SCAN -->|"SendScannedBarcode"| HUB
    HUB -->|"ReceiveScannedBarcode (SignalR)"| VIEWS
```

---

## Hình 2.2 – Sơ đồ phân rã chức năng hệ thống

```mermaid
flowchart TD
    ROOT["Hệ thống quản lý kho hàng quần áo"] --> F1["Xác thực và phân quyền"]
    ROOT --> F2["Quản lý danh mục"]
    ROOT --> F3["Quản lý sản phẩm"]
    ROOT --> F4["Quản lý đối tác"]
    ROOT --> F5["Nghiệp vụ kho"]
    ROOT --> F6["Báo cáo – thống kê"]
    F1 --> F1a["Đăng nhập"]
    F1 --> F1b["Quản lý tài khoản"]
    F1 --> F1c["Gán vai trò"]
    F1 --> F1d["Đặt lại mật khẩu"]
    F2 --> F2a["Nhóm hàng"]
    F2 --> F2b["Màu sắc"]
    F2 --> F2c["Kích cỡ"]
    F2 --> F2d["Vị trí kệ"]
    F3 --> F3a["Tạo sản phẩm"]
    F3 --> F3b["Sinh ma trận biến thể"]
    F3 --> F3c["Sinh / in mã vạch"]
    F3 --> F3d["Tra cứu sản phẩm"]
    F4 --> F4a["Nhà cung cấp"]
    F4 --> F4b["Khách hàng sỉ"]
    F5 --> F5a["Nhập kho"]
    F5 --> F5b["Xuất kho"]
    F5 --> F5c["Quét mã vạch (máy quét / điện thoại)"]
    F5 --> F5d["Tra cứu tồn kho"]
    F5 --> F5e["Cảnh báo tồn thấp"]
    F5 --> F5f["Lịch sử phiếu"]
    F5 --> F5g["Nhật ký biến động"]
    F6 --> F6a["Tổng quan KPI"]
    F6 --> F6b["Biểu đồ nhập – xuất"]
    F6 --> F6c["Xuất Excel tồn kho"]
    F6 --> F6d["Dự báo cần nhập lại"]
```

---

## Hình 2.3 – Sơ đồ use case tổng quát

Mermaid không có dạng use case gốc nên dùng flowchart mô phỏng: lục giác là tác nhân, hình viên thuốc (viên tròn) là use case; cạnh có nhãn `<<include>>`.

```mermaid
flowchart LR
    ADMIN{{"Quản trị viên"}}
    KEEPER{{"Thủ kho"}}
    PHONE{{"Điện thoại (trang quét)"}}
    UC1(["Đăng nhập"])
    UC2(["Quản lý tài khoản"])
    UC3(["Quản lý danh mục"])
    UC4(["Quản lý sản phẩm"])
    UC5(["Quản lý đối tác"])
    UC6(["Lập phiếu nhập / xuất"])
    UC7(["Quét mã vạch"])
    UC8(["Kiểm tra tồn / phiên bản dữ liệu"])
    UC9(["Tra cứu tồn kho"])
    UC10(["In mã vạch"])
    UC11(["Xuất báo cáo Excel"])
    UC12(["Xem nhật ký biến động"])
    UC13(["Xem thống kê"])
    ADMIN --> UC1
    ADMIN --> UC2
    ADMIN --> UC3
    ADMIN --> UC4
    ADMIN --> UC5
    ADMIN --> UC12
    ADMIN --> UC13
    KEEPER --> UC1
    KEEPER --> UC4
    KEEPER --> UC5
    KEEPER --> UC6
    KEEPER --> UC9
    KEEPER --> UC10
    KEEPER --> UC11
    PHONE --> UC7
    UC6 -->|"<<include>>"| UC7
    UC6 -->|"<<include>>"| UC8
```

### (Phương án thay thế – PlantUML use case thật, nếu muốn chuẩn UML)

```plantuml
@startuml
left to right direction
actor "Quản trị viên" as Admin
actor "Thủ kho" as Keeper
actor "Điện thoại (trang quét)" as Phone

rectangle "Hệ thống quản lý kho hàng quần áo" {
  usecase "Đăng nhập" as UC1
  usecase "Quản lý tài khoản" as UC2
  usecase "Quản lý danh mục" as UC3
  usecase "Quản lý sản phẩm" as UC4
  usecase "Quản lý đối tác" as UC5
  usecase "Lập phiếu nhập/xuất" as UC6
  usecase "Quét mã vạch" as UC7
  usecase "Kiểm tra tồn/phiên bản dữ liệu" as UC8
  usecase "Tra cứu tồn kho" as UC9
  usecase "In mã vạch" as UC10
  usecase "Xuất báo cáo Excel" as UC11
  usecase "Xem nhật ký biến động" as UC12
  usecase "Xem thống kê" as UC13
}

Admin --> UC1
Admin --> UC2
Admin --> UC3
Admin --> UC4
Admin --> UC5
Admin --> UC12
Admin --> UC13
Keeper --> UC1
Keeper --> UC4
Keeper --> UC5
Keeper --> UC6
Keeper --> UC9
Keeper --> UC10
Keeper --> UC11
Phone --> UC7
UC6 .> UC7 : <<include>>
UC6 .> UC8 : <<include>>
@enduml
```

---

## Hình 2.4 – Sơ đồ quan hệ thực thể (ERD) của cơ sở dữ liệu

```mermaid
erDiagram
    ROLES ||--o{ USERS : "gán"
    USERS ||--o{ RECEIPTS : "lập phiếu"
    USERS ||--o{ INVENTORY_TRANSACTIONS : "thực hiện"
    CATEGORIES ||--o{ PRODUCTS : "nhóm"
    PRODUCTS ||--o{ PRODUCT_VARIANTS : "có (CASCADE)"
    COLORS ||--o{ PRODUCT_VARIANTS : "màu"
    SIZES ||--o{ PRODUCT_VARIANTS : "kích cỡ"
    PRODUCT_VARIANTS ||--o{ INVENTORIES : "tồn tại (RESTRICT)"
    WAREHOUSE_LOCATIONS ||--o{ INVENTORIES : "chứa (RESTRICT)"
    INVENTORIES ||--o{ INVENTORY_TRANSACTIONS : "ghi nhật ký"
    PRODUCT_VARIANTS ||--o{ INVENTORY_TRANSACTIONS : ""
    WAREHOUSE_LOCATIONS ||--o{ INVENTORY_TRANSACTIONS : ""
    RECEIPTS ||--o{ RECEIPT_DETAILS : "gồm (CASCADE)"
    RECEIPTS |o--o{ INVENTORY_TRANSACTIONS : "tham chiếu"
    PRODUCT_VARIANTS ||--o{ RECEIPT_DETAILS : ""
    WAREHOUSE_LOCATIONS ||--o{ RECEIPT_DETAILS : ""
    SUPPLIERS |o--o{ RECEIPTS : "cung cấp"

    ROLES {
        int RoleId PK
        varchar RoleName UK
        varchar Description
    }
    USERS {
        int UserId PK
        varchar Username UK
        varchar PasswordHash "băm BCrypt"
        varchar FullName
        tinyint IsActive
        int RoleId FK
        datetime CreatedAt
    }
    CATEGORIES {
        int CategoryId PK
        varchar CategoryName
        tinyint IsActive
    }
    PRODUCTS {
        int ProductId PK
        varchar ProductCode UK
        varchar ProductName
        int CategoryId FK
        decimal BasePrice
        varchar Unit
        tinyint IsActive
    }
    COLORS {
        int ColorId PK
        varchar ColorName
        varchar ColorCode
    }
    SIZES {
        int SizeId PK
        varchar SizeName
        int DisplayOrder
    }
    PRODUCT_VARIANTS {
        int VariantId PK
        int ProductId FK
        int ColorId FK
        int SizeId FK
        varchar SKU UK
        varchar Barcode UK
        decimal CostPrice
        decimal SalePrice
        tinyint IsActive
    }
    WAREHOUSE_LOCATIONS {
        int LocationId PK
        varchar LocationCode UK
        varchar LocationName
    }
    INVENTORIES {
        int InventoryId PK
        int VariantId FK
        int LocationId FK
        int QuantityOnHand "CHECK >= 0"
        int SafetyStock
        datetime LastImportDate
        datetime RowVersion "token đồng thời"
    }
    INVENTORY_TRANSACTIONS {
        bigint InventoryTransactionId PK
        int InventoryId FK
        int ReceiptId FK "tùy chọn"
        string TransactionType "IMPORT-EXPORT-ADJUSTMENT"
        int QuantityChange
        int QuantityBefore
        int QuantityAfter "CHECK >= 0"
        varchar ReferenceNo
        datetime OccurredAt
        int PerformedByUserId FK
    }
    SUPPLIERS {
        int SupplierId PK
        varchar SupplierName
        varchar ContactName
        varchar PhoneNumber
        tinyint IsActive
    }
    CUSTOMERS {
        int CustomerId PK
        varchar CustomerCode UK
        varchar CustomerName
        varchar CustomerType
        decimal DebtAmount
        tinyint IsActive
    }
    RECEIPTS {
        int ReceiptId PK
        varchar ReceiptCode UK
        string ReceiptType "IMPORT-EXPORT"
        datetime ReceiptDate
        int SupplierId FK "tùy chọn"
        varchar CustomerName
        decimal TotalAmount
        string Status "DRAFT-COMPLETED-CANCELLED"
        int UserId FK
    }
    RECEIPT_DETAILS {
        int DetailId PK
        int ReceiptId FK
        int VariantId FK
        int LocationId FK
        int Quantity "CHECK > 0"
        decimal UnitPrice
        decimal TotalLineAmount "cột tính toán"
    }
```

---

## Hình 3.1 – Sơ đồ tuần tự xử lý phiếu nhập/xuất kho

```mermaid
sequenceDiagram
    autonumber
    actor User as Thủ kho
    participant WPF as Giao diện WPF - ReceiptView
    participant API as InventoryTransactionsController
    participant SVC as InventoryService
    participant UOW as EfUnitOfWork - giao dịch
    participant DB as MySQL

    User->>WPF: Quét mã vạch, xác nhận phiếu
    WPF->>API: POST /api/inventory/transactions
    API->>SVC: ProcessAsync(request)
    SVC->>UOW: BeginTransactionAsync()
    loop Mỗi nhóm (biến thể, vị trí kệ)
        SVC->>UOW: FindByVariantAndLocationAsync
        UOW->>DB: SELECT Inventories
        DB-->>UOW: Bản ghi tồn + RowVersion
        SVC->>SVC: So ExpectedRowVersion, tính Before/After
    end
    alt Thiếu tồn hoặc lệch phiên bản dữ liệu
        SVC--xAPI: InsufficientInventoryException / InventoryConcurrencyException
        SVC->>UOW: RollbackAsync()
        API-->>WPF: HTTP 409 ProblemDetails
        WPF-->>User: Thông báo lỗi, tải lại số tồn mới
    else Tất cả điều kiện nghiệp vụ đạt
        SVC->>UOW: Cập nhật tồn, thêm InventoryTransaction, Receipt + ReceiptDetails
        UOW->>DB: SaveChangesAsync + COMMIT
        DB-->>UOW: OK
        SVC-->>API: ResultDto (mã phiếu, tồn mới, RowVersion mới)
        API-->>WPF: HTTP 200
        WPF-->>User: Làm mới bảng dòng hàng và số tồn
    end
```

---

## Hình 3.4 – Sơ đồ tuần tự quét mã vạch từ điện thoại qua SignalR

```mermaid
sequenceDiagram
    autonumber
    participant WPF as WPF ReceiptView - HubConnection
    participant HUB as BarcodeScannerHub trên API
    actor Phone as Điện thoại - trang /scan

    WPF->>HUB: Kết nối hub, đăng ký sự kiện ReceiveScannedBarcode
    Phone->>Phone: Camera đọc mã 893600100101
    Phone->>HUB: SendScannedBarcode("893600100101")
    HUB->>HUB: Kiểm tra chuỗi không rỗng
    HUB-->>WPF: ReceiveScannedBarcode("893600100101")
    WPF->>WPF: Điền ô mã vạch, tra biến thể như quét máy quét
```

---

## Ba hình còn lại là ảnh chụp đoạn mã (không phải sơ đồ Mermaid)

Chụp màn hình các đoạn sau từ Visual Studio để chèn vào báo cáo:

**Hình 3.2 – Cấu hình token đồng thời trên entity Inventory** — `Data/WarehouseDbContext.cs` (dòng 117–123):

```csharp
// DATETIME(6) đổi mỗi UPDATE của MySQL; EF dùng giá trị cũ trong WHERE để phát hiện xung đột.
modelBuilder.Entity<Inventory>()
    .Property(x => x.RowVersion)
    .HasColumnType("datetime(6)")
    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

**Hình 3.3 – Băm và đối chiếu mật khẩu** — `Services/WarehouseServices.cs` (dòng 11–16 và 30–37):

```csharp
public static string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}
// ... trong VerifyPassword: chỉ giữ đường BCrypt cho bản vận hành
if (storedHash.StartsWith("$2"))
{
    return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
}
```

**Hình 3.5 – Lớp trung tâm nhận mã vạch từ điện thoại** — `Hubs/BarcodeScannerHub.cs` (dòng 1–18):

```csharp
public class BarcodeScannerHub : Hub
{
    /// <summary>Được gọi từ trang Web trên điện thoại khi camera đọc được mã vạch</summary>
    public async Task SendScannedBarcode(string barcode)
    {
        if (!string.IsNullOrWhiteSpace(barcode))
        {
            await Clients.All.SendAsync("ReceiveScannedBarcode", barcode.Trim());
        }
    }
}
```

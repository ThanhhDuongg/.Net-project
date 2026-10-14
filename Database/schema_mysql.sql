-- ============================================================================
-- HỆ QUẢN TRỊ CSDL: MYSQL (Phiên bản 8.0+)
-- BÀI TẬP LỚN CÔNG NGHỆ .NET: QUẢN LÝ KHO HÀNG QUẦN ÁO
-- Bảng mã: utf8mb4 - Hỗ trợ 100% tiếng Việt
-- ============================================================================

DROP DATABASE IF EXISTS FashionWarehouseDb;
CREATE DATABASE FashionWarehouseDb 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

USE FashionWarehouseDb;

-- 1. BẢNG PHÂN QUYỀN & NGƯỜI DÙNG
CREATE TABLE Roles (
    RoleId INT AUTO_INCREMENT PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255) NULL
) ENGINE=InnoDB;

CREATE TABLE Users (
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NULL,
    PhoneNumber VARCHAR(20) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    RoleId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
) ENGINE=InnoDB;

-- 2. BẢNG THUỘC TÍNH SẢN PHẨM & MA TRẬN BIẾN THỂ (SKU MATRIX)
CREATE TABLE Categories (
    CategoryId INT AUTO_INCREMENT PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE Colors (
    ColorId INT AUTO_INCREMENT PRIMARY KEY,
    ColorName VARCHAR(50) NOT NULL,
    ColorCode VARCHAR(10) NULL
) ENGINE=InnoDB;

CREATE TABLE Sizes (
    SizeId INT AUTO_INCREMENT PRIMARY KEY,
    SizeName VARCHAR(20) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0
) ENGINE=InnoDB;

CREATE TABLE Products (
    ProductId INT AUTO_INCREMENT PRIMARY KEY,
    ProductCode VARCHAR(30) NOT NULL UNIQUE,
    ProductName VARCHAR(200) NOT NULL,
    CategoryId INT NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    Unit VARCHAR(20) NOT NULL DEFAULT 'Cái',
    Description TEXT NULL,
    ImageUrl VARCHAR(500) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(50) NULL,
    UpdatedAt DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(50) NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
) ENGINE=InnoDB;

CREATE TABLE ProductVariants (
    VariantId INT AUTO_INCREMENT PRIMARY KEY,
    ProductId INT NOT NULL,
    ColorId INT NOT NULL,
    SizeId INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    Barcode VARCHAR(50) NOT NULL UNIQUE,
    CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT FK_Variants_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Variants_Colors FOREIGN KEY (ColorId) REFERENCES Colors(ColorId),
    CONSTRAINT FK_Variants_Sizes FOREIGN KEY (SizeId) REFERENCES Sizes(SizeId),
    CONSTRAINT UQ_Variant_Matrix UNIQUE (ProductId, ColorId, SizeId)
) ENGINE=InnoDB;

-- 3. BẢNG VỊ TRÍ & QUẢN LÝ TỒN KHO
CREATE TABLE WarehouseLocations (
    LocationId INT AUTO_INCREMENT PRIMARY KEY,
    LocationCode VARCHAR(30) NOT NULL UNIQUE,
    LocationName VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL
) ENGINE=InnoDB;

CREATE TABLE Inventories (
    InventoryId INT AUTO_INCREMENT PRIMARY KEY,
    VariantId INT NOT NULL,
    LocationId INT NOT NULL,
    QuantityOnHand INT NOT NULL DEFAULT 0,
    SafetyStock INT NOT NULL DEFAULT 10,
    LastImportDate DATETIME NULL,
    RowVersion DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT FK_Inventories_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_Inventories_Locations FOREIGN KEY (LocationId) REFERENCES WarehouseLocations(LocationId),
    CONSTRAINT UQ_Inventory_Variant_Location UNIQUE (VariantId, LocationId),
    CONSTRAINT CK_Inventories_NonNegative CHECK (QuantityOnHand >= 0)
) ENGINE=InnoDB;

-- 4. NHÀ CUNG CẤP & ĐỐI TÁC
CREATE TABLE Suppliers (
    SupplierId INT AUTO_INCREMENT PRIMARY KEY,
    SupplierName VARCHAR(150) NOT NULL,
    ContactName VARCHAR(100) NULL,
    PhoneNumber VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    Address VARCHAR(255) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

-- 5. GIAO DỊCH XUẤT NHẬP KHO
CREATE TABLE Receipts (
    ReceiptId INT AUTO_INCREMENT PRIMARY KEY,
    ReceiptCode VARCHAR(30) NOT NULL UNIQUE,
    ReceiptType ENUM('IMPORT', 'EXPORT') NOT NULL,
    ReceiptDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SupplierId INT NULL,
    CustomerName VARCHAR(100) NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes VARCHAR(500) NULL,
    Status ENUM('DRAFT', 'COMPLETED', 'CANCELLED') NOT NULL DEFAULT 'COMPLETED',
    UserId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(50) NULL,
    CONSTRAINT FK_Receipts_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers(SupplierId),
    CONSTRAINT FK_Receipts_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
) ENGINE=InnoDB;

CREATE TABLE ReceiptDetails (
    DetailId INT AUTO_INCREMENT PRIMARY KEY,
    ReceiptId INT NOT NULL,
    VariantId INT NOT NULL,
    LocationId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalLineAmount DECIMAL(18,2) GENERATED ALWAYS AS (Quantity * UnitPrice) STORED,
    CONSTRAINT FK_ReceiptDetails_Receipts FOREIGN KEY (ReceiptId) REFERENCES Receipts(ReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_ReceiptDetails_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_ReceiptDetails_Locations FOREIGN KEY (LocationId) REFERENCES WarehouseLocations(LocationId),
    CONSTRAINT CK_ReceiptDetails_PositiveQuantity CHECK (Quantity > 0)
) ENGINE=InnoDB;

-- 6. CHỈ MỤC (INDEXES)
CREATE INDEX IX_ProductVariants_Barcode ON ProductVariants(Barcode);
CREATE INDEX IX_Inventories_Variant_Location ON Inventories(VariantId, LocationId);
CREATE INDEX IX_Receipts_Date_Type ON Receipts(ReceiptDate, ReceiptType);

-- 7. SEED DATA
INSERT INTO Roles (RoleName, Description) VALUES 
('Admin', 'Quản trị viên toàn quyền hệ thống'),
('WarehouseKeeper', 'Thủ kho phụ trách xuất nhập');

INSERT INTO Users (Username, PasswordHash, FullName, RoleId) VALUES 
('admin', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Quản Trị Viên', 1),
('thukho', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Trần Văn Kho', 2);

INSERT INTO Categories (CategoryName, Description) VALUES 
('Áo Thun / T-Shirt', 'Các mẫu áo phông cộc tay và dài tay'),
('Áo Sơ Mi', 'Sơ mi oxford, sơ mi công sở'),
('Quần Jeans', 'Quần bò nam nữ dáng suông và ôm');

INSERT INTO Colors (ColorName, ColorCode) VALUES 
('Trắng', '#FFFFFF'),
('Đen', '#000000'),
('Xanh Navy', '#000080');

INSERT INTO Sizes (SizeName, DisplayOrder) VALUES 
('S', 1), ('M', 2), ('L', 3), ('XL', 4);

INSERT INTO WarehouseLocations (LocationCode, LocationName, Description) VALUES 
('KHO-A-01', 'Kệ A - Tầng 1', 'Khu vực chuyên áo thun'),
('KHO-B-01', 'Kệ B - Tầng 1', 'Khu vực chuyên áo sơ mi');

INSERT INTO Suppliers (SupplierName, PhoneNumber, Email, Address) VALUES 
('Công Ty May Mặc Việt Tiến', '02838640800', 'viettien@supplier.vn', 'Quận Tân Bình, TP.HCM'),
('Tổng Công Ty Đức Giang', '02438271544', 'ducgiang@supplier.vn', 'Long Biên, Hà Nội');

INSERT INTO Products (ProductCode, ProductName, CategoryId, BasePrice, Unit) VALUES 
('POLO-001', 'Áo Polo Nam Classic Cotton', 1, 250000, 'Cái');

INSERT INTO ProductVariants (ProductId, ColorId, SizeId, SKU, Barcode, CostPrice, SalePrice) VALUES 
(1, 2, 2, 'POLO01-BLK-M', '893600100101', 120000, 250000),
(1, 2, 3, 'POLO01-BLK-L', '893600100102', 120000, 250000),
(1, 1, 2, 'POLO01-WHT-M', '893600100103', 120000, 250000),
(1, 1, 3, 'POLO01-WHT-L', '893600100104', 120000, 250000);

INSERT INTO Inventories (VariantId, LocationId, QuantityOnHand, SafetyStock, LastImportDate) VALUES 
(1, 1, 50, 10, NOW()),
(2, 1, 40, 10, NOW()),
(3, 1, 30, 10, NOW()),
(4, 1, 20, 10, NOW());

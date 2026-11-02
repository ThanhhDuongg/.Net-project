-- ================================================
-- SEED USERS: Chạy script này 1 lần trong MySQL
-- để tạo tài khoản đăng nhập mặc định.
-- ================================================

-- Bước 1: Đảm bảo đã có Role
INSERT IGNORE INTO Roles (RoleName, Description) VALUES
('Admin',          'Quản trị viên toàn hệ thống'),
('WarehouseKeeper','Thủ kho - nhập xuất kho hàng ngày');

-- Bước 2: Seed tài khoản admin (password = "password")
-- Hash BCrypt workfactor 11 của chuỗi "password":
INSERT IGNORE INTO Users (Username, PasswordHash, FullName, RoleId, IsActive, CreatedBy) VALUES
(
  'admin',
  '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi',
  'Quản Trị Viên',
  (SELECT RoleId FROM Roles WHERE RoleName = 'Admin' LIMIT 1),
  1,
  'system'
),
(
  'thukho',
  '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi',
  'Trần Văn Kho',
  (SELECT RoleId FROM Roles WHERE RoleName = 'WarehouseKeeper' LIMIT 1),
  1,
  'system'
);

-- Kiểm tra kết quả
SELECT UserId, Username, FullName, RoleId, IsActive FROM Users;

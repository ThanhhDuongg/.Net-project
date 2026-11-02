using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp.Views;

public partial class EmployeeView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(6)
    };

    private List<UserListItemDto> _rawUsers = new();
    private List<UserDisplayModel> _displayedUsers = new();
    private int? _editingUserId = null;
    private UserListItemDto? _resetTargetUser = null;

    // Fallback data khi chưa bật API hoặc offline
    private static readonly List<UserListItemDto> FallbackUsers = new()
    {
        new()
        {
            UserId = 1,
            Username = "admin",
            FullName = "Nguyễn Quản Trị",
            Email = "admin@fashionwms.vn",
            PhoneNumber = "0901234567",
            IsActive = true,
            RoleId = 1,
            RoleName = "Admin",
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            TotalReceiptsCreated = 28
        },
        new()
        {
            UserId = 2,
            Username = "thukho",
            FullName = "Trần Văn Kho",
            Email = "thukho@fashionwms.vn",
            PhoneNumber = "0912345678",
            IsActive = true,
            RoleId = 2,
            RoleName = "WarehouseKeeper",
            CreatedAt = DateTime.UtcNow.AddDays(-45),
            TotalReceiptsCreated = 42
        },
        new()
        {
            UserId = 3,
            Username = "ketoankho",
            FullName = "Lê Thị Thu Thủy",
            Email = "ketoan@fashionwms.vn",
            PhoneNumber = "0987654321",
            IsActive = true,
            RoleId = 2,
            RoleName = "WarehouseKeeper",
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            TotalReceiptsCreated = 15
        },
        new()
        {
            UserId = 4,
            Username = "nhanvien_tamkhoa",
            FullName = "Hoàng Minh Tuấn",
            Email = "tuan.hm@fashionwms.vn",
            PhoneNumber = "0977889900",
            IsActive = false,
            RoleId = 2,
            RoleName = "WarehouseKeeper",
            CreatedAt = DateTime.UtcNow.AddDays(-90),
            TotalReceiptsCreated = 6
        }
    };

    public EmployeeView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyRolePermissions();
            _ = LoadUsersAsync();
        };
    }

    /// <summary>
    /// Kiểm tra vai trò của người dùng hiện tại để điều chỉnh quyền tương tác trên giao diện.
    /// </summary>
    private void ApplyRolePermissions()
    {
        if (brdPermissionWarning == null || txtPermissionWarning == null || btnAddEmployee == null) return;
        bool isAdmin = string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            brdPermissionWarning.Visibility = Visibility.Visible;
            txtPermissionWarning.Text = $"Chế độ chỉ xem: Bạn đang đăng nhập bằng tài khoản '{UserSession.RoleName}'. Chỉ Quản trị viên (Admin) mới có quyền tạo mới, sửa đổi vai trò hoặc khóa tài khoản.";
            btnAddEmployee.IsEnabled = false;
            btnAddEmployee.Opacity = 0.6;
        }
        else
        {
            brdPermissionWarning.Visibility = Visibility.Collapsed;
            btnAddEmployee.IsEnabled = true;
            btnAddEmployee.Opacity = 1.0;
        }
    }

    /// <summary>
    /// Tải danh sách nhân sự từ API hoặc dữ liệu dự phòng nếu chưa có kết nối.
    /// </summary>
    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await HttpClient.GetFromJsonAsync<List<UserListItemDto>>("api/users");
            _rawUsers = (response != null && response.Count > 0) ? response : new List<UserListItemDto>(FallbackUsers);
        }
        catch
        {
            _rawUsers = new List<UserListItemDto>(FallbackUsers);
        }

        UpdateKpis();
        ApplyFilter();
    }

    /// <summary>
    /// Cập nhật 4 thẻ KPI thống kê nhân sự.
    /// </summary>
    private void UpdateKpis()
    {
        if (txtTotalUsers == null || txtActiveUsers == null || txtLockedUsers == null || txtRoleStats == null || _rawUsers == null) return;
        int total = _rawUsers.Count;
        int active = _rawUsers.Count(u => u.IsActive);
        int locked = total - active;
        int adminCount = _rawUsers.Count(u => string.Equals(u.RoleName, "Admin", StringComparison.OrdinalIgnoreCase));
        int keeperCount = total - adminCount;

        txtTotalUsers.Text = total.ToString();
        txtActiveUsers.Text = active.ToString();
        txtLockedUsers.Text = locked.ToString();
        txtRoleStats.Text = $"{adminCount} Admin • {keeperCount} Thủ kho";
    }

    /// <summary>
    /// Lọc danh sách nhân viên theo từ khóa tìm kiếm, vai trò và trạng thái.
    /// </summary>
    private void ApplyFilter()
    {
        if (!IsLoaded || dgEmployees == null || txtListSummary == null || _rawUsers == null) return;
        var query = _rawUsers.AsEnumerable();

        // 1. Tìm theo từ khóa
        var keyword = txtSearch?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(u =>
                (u.Username?.ToLowerInvariant().Contains(keyword) ?? false) ||
                (u.FullName?.ToLowerInvariant().Contains(keyword) ?? false) ||
                (u.Email?.ToLowerInvariant().Contains(keyword) ?? false) ||
                (u.PhoneNumber?.ToLowerInvariant().Contains(keyword) ?? false));
        }

        // 2. Lọc theo chức vụ
        if (cmbRoleFilter?.SelectedIndex > 0)
        {
            if (cmbRoleFilter.SelectedIndex == 1) // Admin
                query = query.Where(u => string.Equals(u.RoleName, "Admin", StringComparison.OrdinalIgnoreCase));
            else if (cmbRoleFilter.SelectedIndex == 2) // WarehouseKeeper
                query = query.Where(u => !string.Equals(u.RoleName, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        // 3. Lọc theo trạng thái
        if (cmbStatusFilter?.SelectedIndex > 0)
        {
            if (cmbStatusFilter.SelectedIndex == 1) // Hoạt động
                query = query.Where(u => u.IsActive);
            else if (cmbStatusFilter.SelectedIndex == 2) // Khóa
                query = query.Where(u => !u.IsActive);
        }

        bool isAdmin = string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        _displayedUsers = query.Select(u => new UserDisplayModel(u, isAdmin, UserSession.UserId)).ToList();
        dgEmployees.ItemsSource = _displayedUsers;
        txtListSummary.Text = $"Hiển thị {_displayedUsers.Count} / {_rawUsers.Count} nhân sự";
    }

    // ==================== SEARCH & FILTER EVENTS ====================
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) ApplyFilter(); }
    private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) ApplyFilter(); }
    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();

    // ==================== MODAL 1: ADD / EDIT EMPLOYEE ====================
    private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Chỉ Quản trị viên (Admin) mới có quyền tạo thêm tài khoản nhân viên.",
                "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _editingUserId = null;
        txtModalTitle.Text = "+ Thêm Nhân Viên Mới";
        txtModalSubtitle.Text = "Điền thông tin tài khoản và thiết lập quyền truy cập hệ thống";
        btnSaveUser.Content = "💾 Lưu Nhân Viên";

        txtModalFullName.Text = string.Empty;
        txtModalUsername.Text = string.Empty;
        txtModalUsername.IsReadOnly = false;
        txtModalPassword.Password = string.Empty;
        pnlPasswordSection.Visibility = Visibility.Visible;
        cmbModalRole.SelectedIndex = 1; // Default to WarehouseKeeper
        txtModalEmail.Text = string.Empty;
        txtModalPhone.Text = string.Empty;
        chkModalActive.IsChecked = true;
        brdModalError.Visibility = Visibility.Collapsed;

        pnlUserModal.Visibility = Visibility.Visible;
        txtModalFullName.Focus();
    }

    private void BtnRowEdit_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Chỉ Quản trị viên (Admin) mới có quyền chỉnh sửa thông tin nhân viên.",
                "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sender is Button btn && btn.Tag is UserDisplayModel displayModel)
        {
            var user = _rawUsers.FirstOrDefault(u => u.UserId == displayModel.UserId);
            if (user == null) return;

            _editingUserId = user.UserId;
            txtModalTitle.Text = $"✏️ Cập Nhật Thông Tin: {user.Username}";
            txtModalSubtitle.Text = "Chỉnh sửa họ tên, quyền hạn, email, số điện thoại hoặc trạng thái";
            btnSaveUser.Content = "💾 Cập Nhật";

            txtModalFullName.Text = user.FullName;
            txtModalUsername.Text = user.Username;
            txtModalUsername.IsReadOnly = true; // Không cho sửa username
            pnlPasswordSection.Visibility = Visibility.Collapsed; // Đổi mật khẩu qua modal riêng
            cmbModalRole.SelectedIndex = string.Equals(user.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            txtModalEmail.Text = user.Email ?? string.Empty;
            txtModalPhone.Text = user.PhoneNumber ?? string.Empty;
            chkModalActive.IsChecked = user.IsActive;
            brdModalError.Visibility = Visibility.Collapsed;

            pnlUserModal.Visibility = Visibility.Visible;
            txtModalFullName.Focus();
        }
    }

    private void BtnCloseUserModal_Click(object sender, RoutedEventArgs e)
    {
        pnlUserModal.Visibility = Visibility.Collapsed;
    }

    private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
    {
        var fullName = txtModalFullName.Text.Trim();
        var username = txtModalUsername.Text.Trim();
        var email = string.IsNullOrWhiteSpace(txtModalEmail.Text) ? null : txtModalEmail.Text.Trim();
        var phone = string.IsNullOrWhiteSpace(txtModalPhone.Text) ? null : txtModalPhone.Text.Trim();
        int roleId = cmbModalRole.SelectedIndex == 0 ? 1 : 2;
        bool isActive = chkModalActive.IsChecked ?? true;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowModalError("Vui lòng nhập họ và tên nhân viên.");
            return;
        }

        if (_editingUserId == null)
        {
            // Thêm mới
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                ShowModalError("Tên đăng nhập phải có ít nhất 3 ký tự.");
                return;
            }
            if (username.Contains(' '))
            {
                ShowModalError("Tên đăng nhập không được chứa khoảng trắng.");
                return;
            }

            var password = txtModalPassword.Password;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ShowModalError("Mật khẩu khởi tạo phải có ít nhất 6 ký tự.");
                return;
            }

            var createDto = new CreateUserDto
            {
                Username = username,
                Password = password,
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                RoleId = roleId,
                IsActive = isActive
            };

            try
            {
                var response = await HttpClient.PostAsJsonAsync("api/users", createDto);
                if (response.IsSuccessStatusCode)
                {
                    pnlUserModal.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Đã tạo mới tài khoản '{username}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUsersAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ShowModalError($"Lỗi máy chủ: {error}");
                }
            }
            catch (Exception ex)
            {
                ShowModalError($"Không thể kết nối đến máy chủ: {ex.Message}");
            }
        }
        else
        {
            // Cập nhật
            var updateDto = new UpdateUserDto
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                RoleId = roleId,
                IsActive = isActive
            };

            try
            {
                var response = await HttpClient.PutAsJsonAsync($"api/users/{_editingUserId.Value}", updateDto);
                if (response.IsSuccessStatusCode)
                {
                    pnlUserModal.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUsersAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ShowModalError($"Lỗi máy chủ: {error}");
                }
            }
            catch (Exception ex)
            {
                ShowModalError($"Không thể kết nối đến máy chủ: {ex.Message}");
            }
        }
    }

    private void ShowModalError(string message)
    {
        txtModalError.Text = message;
        brdModalError.Visibility = Visibility.Visible;
    }

    // ==================== MODAL 2: RESET PASSWORD ====================
    private void BtnRowResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Chỉ Quản trị viên (Admin) mới có quyền đặt lại mật khẩu nhân viên.",
                "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sender is Button btn && btn.Tag is UserDisplayModel displayModel)
        {
            var user = _rawUsers.FirstOrDefault(u => u.UserId == displayModel.UserId);
            if (user == null) return;

            _resetTargetUser = user;
            txtResetTargetUser.Text = $"Nhân viên: {user.FullName} ({user.Username})";
            txtNewPassword.Text = string.Empty;
            brdResetError.Visibility = Visibility.Collapsed;

            pnlResetPasswordModal.Visibility = Visibility.Visible;
            txtNewPassword.Focus();
        }
    }

    private void BtnCloseResetModal_Click(object sender, RoutedEventArgs e)
    {
        pnlResetPasswordModal.Visibility = Visibility.Collapsed;
    }

    private void BtnGeneratePassword_Click(object sender, RoutedEventArgs e)
    {
        txtNewPassword.Text = "FwPass@2026";
    }

    private async void BtnConfirmResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (_resetTargetUser == null) return;

        var newPassword = txtNewPassword.Text.Trim();
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            txtResetError.Text = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            brdResetError.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var resetDto = new ResetPasswordDto { NewPassword = newPassword };
            var response = await HttpClient.PostAsJsonAsync($"api/users/{_resetTargetUser.UserId}/reset-password", resetDto);

            if (response.IsSuccessStatusCode)
            {
                pnlResetPasswordModal.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Đã đặt lại mật khẩu cho tài khoản '{_resetTargetUser.Username}' thành công!\nMật khẩu mới: {newPassword}",
                    "Đổi mật khẩu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                txtResetError.Text = $"Lỗi: {error}";
                brdResetError.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            txtResetError.Text = $"Lỗi kết nối: {ex.Message}";
            brdResetError.Visibility = Visibility.Visible;
        }
    }

    // ==================== LOCK / UNLOCK TOGGLE ====================
    private async void BtnRowToggleLock_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(UserSession.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Chỉ Quản trị viên (Admin) mới có quyền khóa hoặc mở khóa tài khoản.",
                "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sender is Button btn && btn.Tag is UserDisplayModel displayModel)
        {
            var user = _rawUsers.FirstOrDefault(u => u.UserId == displayModel.UserId);
            if (user == null) return;

            if (user.UserId == UserSession.UserId)
            {
                MessageBox.Show("Bạn không thể tự khóa tài khoản của chính mình khi đang đăng nhập!",
                    "Cảnh báo bảo mật", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actionName = user.IsActive ? "KHÓA" : "MỞ KHÓA";
            var confirmResult = MessageBox.Show(
                $"Bạn có chắc chắn muốn {actionName} tài khoản '{user.Username}' ({user.FullName})?",
                $"Xác nhận {actionName}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var response = await HttpClient.PatchAsync($"api/users/{user.UserId}/toggle-status", null);
                if (response.IsSuccessStatusCode)
                {
                    await LoadUsersAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể thực hiện: {error}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ==================== MODAL 3: ROLE PERMISSION MATRIX ====================
    private async void BtnShowPermissionMatrix_Click(object sender, RoutedEventArgs e)
    {
        pnlPermissionMatrixModal.Visibility = Visibility.Visible;
        try
        {
            var matrix = await HttpClient.GetFromJsonAsync<List<RolePermissionMatrixDto>>("api/roles/permissions");
            if (matrix != null && matrix.Count > 0)
            {
                dgPermissions.ItemsSource = matrix.Select(m => new PermissionMatrixDisplayModel(m)).ToList();
                return;
            }
        }
        catch
        {
            // Fallback static matrix
        }

        dgPermissions.ItemsSource = GetFallbackPermissionMatrix().Select(m => new PermissionMatrixDisplayModel(m)).ToList();
    }

    private void BtnClosePermissionModal_Click(object sender, RoutedEventArgs e)
    {
        pnlPermissionMatrixModal.Visibility = Visibility.Collapsed;
    }

    private static List<RolePermissionMatrixDto> GetFallbackPermissionMatrix() => new()
    {
        new() { ModuleName = "Tổng quan (Dashboard)", FeatureName = "Xem tổng quan tồn kho & biểu đồ nhập xuất", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Theo dõi số lượng hàng hóa, cảnh báo hết hàng" },
        new() { ModuleName = "Tổng quan (Dashboard)", FeatureName = "Xem tổng giá trị vốn tồn kho", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Số liệu tài chính giá vốn hàng trong kho" },
        new() { ModuleName = "Quản lý Sản phẩm & SKU", FeatureName = "Xem danh mục sản phẩm, biến thể & Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Tra cứu thông tin hàng hóa, size, màu sắc" },
        new() { ModuleName = "Quản lý Sản phẩm & SKU", FeatureName = "Tạo mới sản phẩm & sinh ma trận biến thể", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Khai báo sản phẩm mới vào danh mục kho" },
        new() { ModuleName = "Quản lý Danh mục", FeatureName = "Thêm, sửa, xóa danh mục sản phẩm", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Cấu trúc cây danh mục hàng hóa (Áo sơ mi, Quần Jean,...)" },
        new() { ModuleName = "Nhập / Xuất Kho", FeatureName = "Tạo phiếu nhập kho (NCC) & quét Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Nhận hàng từ nhà cung cấp vào kho" },
        new() { ModuleName = "Nhập / Xuất Kho", FeatureName = "Tạo phiếu xuất kho & quét Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xuất hàng bán sỉ/lẻ hoặc chuyển kho nội bộ" },
        new() { ModuleName = "Tồn kho & Vị trí kệ", FeatureName = "Tra cứu tồn kho thực tế theo kệ hàng", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xem vị trí kệ lưu trữ (Khu A, B, C, D) và số lượng" },
        new() { ModuleName = "Tồn kho & Vị trí kệ", FeatureName = "Xuất bảng kiểm kê tồn kho ra Excel", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Hỗ trợ công tác kiểm kê định kỳ" },
        new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Xem sổ thẻ kho chi tiết theo ngày", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Theo dõi biến động xuất nhập tồn từng SKU" },
        new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Dự báo nhập hàng bổ sung tự động", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Gợi ý số lượng đặt hàng dựa trên ngưỡng an toàn" },
        new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Xuất báo cáo tổng hợp tài chính ra Excel", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Báo cáo doanh số và biến động giá trị tài sản kho" },
        new() { ModuleName = "Nhân viên & Phân quyền", FeatureName = "Xem danh sách nhân sự kho", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xem danh sách và thông tin liên hệ các thành viên" },
        new() { ModuleName = "Nhân viên & Phân quyền", FeatureName = "Tạo tài khoản, phân quyền & khóa tài khoản", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Quản lý tài khoản, đặt lại mật khẩu và cấp quyền truy cập" },
    };
}

// ==================== VIEW MODELS / DISPLAY WRAPPERS ====================

public class UserDisplayModel
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalReceiptsCreated { get; set; }

    public string Initials { get; }
    public Brush AvatarBg { get; }
    public Brush AvatarFg { get; }
    public string UserCodeTag { get; }
    public string EmailDisplay { get; }
    public string PhoneDisplay { get; }
    public string ReceiptsDisplay { get; }
    public string RoleBadgeText { get; }
    public Brush RoleBadgeBg { get; }
    public Brush RoleBadgeFg { get; }
    public string StatusText { get; }
    public Brush StatusBadgeBg { get; }
    public Brush StatusBadgeFg { get; }
    public string CreatedDateDisplay { get; }
    public string LockActionText { get; }
    public Brush LockActionFg { get; }
    public bool CanManage { get; }
    public bool CanToggleLock { get; }

    public UserDisplayModel(UserListItemDto dto, bool currentUserIsAdmin, int currentUserId)
    {
        UserId = dto.UserId;
        Username = dto.Username;
        FullName = dto.FullName;
        Email = dto.Email;
        PhoneNumber = dto.PhoneNumber;
        IsActive = dto.IsActive;
        RoleId = dto.RoleId;
        RoleName = dto.RoleName;
        CreatedAt = dto.CreatedAt;
        TotalReceiptsCreated = dto.TotalReceiptsCreated;

        // Initials (e.g. "QT" or "TV")
        var words = (dto.FullName ?? dto.Username).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
            Initials = $"{words[0][0]}{words[^1][0]}".ToUpperInvariant();
        else if (words.Length == 1 && words[0].Length >= 2)
            Initials = words[0][..2].ToUpperInvariant();
        else
            Initials = "FW";

        bool isAdmin = string.Equals(dto.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        // Avatar styling
        if (isAdmin)
        {
            AvatarBg = new SolidColorBrush(Color.FromRgb(219, 234, 254)); // #DBEAFE
            AvatarFg = new SolidColorBrush(Color.FromRgb(30, 64, 175));   // #1E40AF
            RoleBadgeText = "👑 Quản trị viên (Admin)";
            RoleBadgeBg = new SolidColorBrush(Color.FromRgb(238, 242, 255)); // #EEF2FF
            RoleBadgeFg = new SolidColorBrush(Color.FromRgb(67, 56, 202));   // #4338CA
        }
        else
        {
            AvatarBg = new SolidColorBrush(Color.FromRgb(209, 250, 229)); // #D1FAE5
            AvatarFg = new SolidColorBrush(Color.FromRgb(6, 95, 70));     // #065F46
            RoleBadgeText = "📦 Thủ kho (WarehouseKeeper)";
            RoleBadgeBg = new SolidColorBrush(Color.FromRgb(236, 253, 245)); // #ECFDF5
            RoleBadgeFg = new SolidColorBrush(Color.FromRgb(4, 120, 87));    // #047857
        }

        UserCodeTag = $"ID: #{dto.UserId:D3}";
        EmailDisplay = string.IsNullOrWhiteSpace(dto.Email) ? "—" : dto.Email;
        PhoneDisplay = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? "—" : dto.PhoneNumber;
        ReceiptsDisplay = $"{dto.TotalReceiptsCreated} phiếu";

        // Status pill
        if (dto.IsActive)
        {
            StatusText = "● Đang hoạt động";
            StatusBadgeBg = new SolidColorBrush(Color.FromRgb(222, 247, 236)); // #DEF7EC
            StatusBadgeFg = new SolidColorBrush(Color.FromRgb(3, 84, 63));     // #03543F
            LockActionText = "🔒 Khóa";
            LockActionFg = new SolidColorBrush(Color.FromRgb(220, 38, 38));    // #DC2626
        }
        else
        {
            StatusText = "● Đã tạm khóa";
            StatusBadgeBg = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // #FEE2E2
            StatusBadgeFg = new SolidColorBrush(Color.FromRgb(153, 27, 27));   // #991B1B
            LockActionText = "🔓 Mở khóa";
            LockActionFg = new SolidColorBrush(Color.FromRgb(5, 150, 105));    // #059669
        }

        CreatedDateDisplay = dto.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        // Permission flags
        CanManage = currentUserIsAdmin;
        CanToggleLock = currentUserIsAdmin && (dto.UserId != currentUserId);
    }
}

public class PermissionMatrixDisplayModel
{
    public string ModuleName { get; }
    public string FeatureName { get; }
    public string Description { get; }
    public string WhBadgeText { get; }
    public Brush WhBadgeBg { get; }
    public Brush WhBadgeFg { get; }

    public PermissionMatrixDisplayModel(RolePermissionMatrixDto dto)
    {
        ModuleName = dto.ModuleName;
        FeatureName = dto.FeatureName;
        Description = dto.Description;

        if (dto.WarehouseKeeperAllowed)
        {
            WhBadgeText = "✅ Cho phép";
            WhBadgeBg = new SolidColorBrush(Color.FromRgb(209, 250, 229)); // #D1FAE5
            WhBadgeFg = new SolidColorBrush(Color.FromRgb(6, 95, 70));     // #065F46
        }
        else
        {
            WhBadgeText = "⛔ Giới hạn";
            WhBadgeBg = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // #FEE2E2
            WhBadgeFg = new SolidColorBrush(Color.FromRgb(153, 27, 27));   // #991B1B
        }
    }
}

public class UserListItemDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalReceiptsCreated { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class RolePermissionMatrixDto
{
    public string ModuleName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool AdminAllowed { get; set; }
    public bool WarehouseKeeperAllowed { get; set; }
    public string Description { get; set; } = string.Empty;
}
